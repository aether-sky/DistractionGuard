﻿using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Transactions;

namespace DistractionGuard
{
  internal class Model
  {
    const string DatabaseFile = "distractionGuardData.db";
    const string ConnectionString = "Data Source=" + DatabaseFile;//;New=False;

    static SqliteConnection GetConnection() {
      return new SqliteConnection(ConnectionString);
    }

    static internal Model Load()
    {
      using var con = GetConnection();
      con.Open();
      string qConfig = @"
          CREATE TABLE IF NOT EXISTS Config (
            Name TEXT NOT NULL,
            Value TEXT NOT NULL
          );";
      string qPatterns = @"
          CREATE TABLE IF NOT EXISTS Patterns (
              Id INTEGER PRIMARY KEY AUTOINCREMENT,
              Pattern TEXT NOT NULL,
              Seconds INTEGER NOT NULL
          );";
      var tableQueries = new List<string>() { qPatterns, qConfig };
      foreach (var q in tableQueries) {
        using (var command = new SqliteCommand(q, con))
        {
          command.ExecuteNonQuery();
        }
      }

      var config = GetConfig(con);
      var patterns = GetPatterns(con);
      con.Close();
      var result = new Model(config, patterns);
      Console.WriteLine($"Loaded {result}");
      return new Model(config, patterns);

    }

    static Dictionary<string, int> GetPatterns(SqliteConnection con)
    {
      var result = new Dictionary<string, int>();
      string query = "SELECT Pattern, Seconds FROM Patterns";
      using var cmd = new SqliteCommand(query, con);
      using var read = cmd.ExecuteReader();
      while (read.Read())
      {
        string pattern = read.GetString(0);
        int seconds = read.GetInt32(1);
        result[pattern] = seconds;
      }
      return result;
    }

    static Dictionary<string, string> GetConfig(SqliteConnection con)
    {
      var result = new Dictionary<string, string>();
      string query = "SELECT Name, Value FROM Config";
      using var cmd = new SqliteCommand(query, con);
      using var read = cmd.ExecuteReader();
      while (read.Read())
      {
        string name = read.GetString(0);
        string value = read.GetString(1);
        result[name] = value;
      }
      return result;
    }

    static void Save(Model m)
    {
      using var con = GetConnection();
      con.Open();
      using var trans = con.BeginTransaction();
      try
      {
        ClearTables(con, trans);
        SavePatterns(m, con, trans);
        SaveConfig(m, con, trans);
      }
      catch (Exception e)
      {
        // If something goes wrong, rollback the transaction
        trans.Rollback();
        Console.WriteLine($"Save failed: {e.Message}");

      }
    }

    static void ClearTables(SqliteConnection con, SqliteTransaction trans)
    {
      var queries = new List<string> { "DELETE FROM Patterns", "DELETE FROM Config" };
      foreach (var q in queries)
      {
        var query = new SqliteCommand(q, con, trans);
        query.ExecuteNonQuery();
      }
    }
    static void SavePatterns(Model m, SqliteConnection con, SqliteTransaction trans)
    {
      var query = "INSERT INTO Patterns(Pattern, Seconds) VALUES(@Pattern, @Seconds)";
      foreach (var p in m.patterns)
      {
        using var cmd = new SqliteCommand(query, con, trans);
        cmd.Parameters.AddWithValue("@Pattern", p.Key);
        cmd.Parameters.AddWithValue("@Seconds", p.Value);
        cmd.ExecuteNonQuery();
      }
    }
    static void SaveConfig(Model m, SqliteConnection con, SqliteTransaction trans)
    {
      var query = "INSERT INTO Config(Name, Value) VALUES(@Name, @Value)";
      foreach (var p in m.config)
      {
        using var cmd = new SqliteCommand(query, con, trans);
        cmd.Parameters.AddWithValue("@Name", p.Key);
        cmd.Parameters.AddWithValue("@Value", p.Value);
        cmd.ExecuteNonQuery();
      }
    }


    private Dictionary<string, string> config;
    private Dictionary<string, int> patterns;
    public Model(Dictionary<string, string> config, Dictionary<string, int> patterns)
    {
      this.config = config;
      this.patterns = patterns;
      if (config.Count == 0 && patterns.Count == 0) //insert some dummy items
      {

        Console.WriteLine($"Creating dummy model");
        config["other"] = "1";
        patterns[".*Discord.*"] = 15;
        patterns[".*Brave.*"] = 15;
        Save(this);
      }

    }

    public override string ToString()
    {
      var sb = new StringBuilder();
      sb.Append("Model(patterns {");
      foreach (var p in patterns)
      {
        sb.Append($"{p.Key}->{p.Value},");
      }
      sb.Append("}, config {");
      foreach (var c in config)
      {
        sb.Append($"{c.Key}->{c.Value}");
      }
      sb.Append("})");
      return sb.ToString();

    }

    public void PopulateList(ListView list)
    {
      list.Clear();
      list.Columns.Add("Pattern", 100); // Width 100
      list.Columns.Add("Seconds", 80);  // Width 80
      list.Columns.Add("Windows Matching", 80);  // Width 80
      foreach (var p in patterns)
      {
        var item = new ListViewItem(p.Key);
        item.SubItems.Add(p.Value.ToString());
        item.SubItems.Add("todo");
        list.Items.Add(item);
      }

    }
  }
}