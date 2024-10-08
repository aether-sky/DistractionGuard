using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Transactions;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DistractionGuard
{

  internal static class Model
  {

    internal class ModelImpl
    {
      internal Dictionary<string, string> config;
      internal Dictionary<string, int> patterns;
      public ModelImpl(Dictionary<string, string> config, Dictionary<string, int> patterns)
      {
        this.config = config;
        this.patterns = patterns;

      }

      public void LoadDummyValues()
      {
        config["other"] = "1";
        patterns[".*Discord.*"] = 15;
        patterns[".*Brave.*"] = 15;
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

      internal void AddPattern(string text, int seconds)
      {
        patterns[text] = seconds;
      }
    }

    static readonly ModelImpl model;
    static Model() 
    {
      model = Load();
      Save();
    }
    const string DatabaseFile = "distractionGuardData.db";
    const string ConnectionString = "Data Source=" + DatabaseFile;//;New=False;
    static SqliteConnection GetConnection() {
      return new SqliteConnection(ConnectionString);
    }

    private static ModelImpl Load()
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
      var result = new ModelImpl(config, patterns);
      if (result.config.Count == 0 && result.patterns.Count == 0) //insert some dummy items
      {

        Console.WriteLine($"Creating dummy model");
        result.LoadDummyValues();

      }
      Console.WriteLine($"Loaded {result}");
      return result;

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
        if (Regex.Replace(pattern, @"\s+", "").Length > 0)
        {
          int seconds = read.GetInt32(1);
          result[pattern] = seconds;
        }
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

    internal static void Save()
    {
      Globals.Debug($"saving {Model.model}");
      using var con = GetConnection();
      con.Open();
      using var trans = con.BeginTransaction();
      try
      {
        ClearTables(con, trans);
        SavePatterns(Model.model, con, trans);
        SaveConfig(Model.model, con, trans);
      }
      catch (Exception e)
      {
        // If something goes wrong, rollback the transaction
        trans.Rollback();
        Console.WriteLine($"Save failed: {e.Message}");

      }
      trans.Commit();
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
    static void SavePatterns(ModelImpl m, SqliteConnection con, SqliteTransaction trans)
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
    static void SaveConfig(ModelImpl m, SqliteConnection con, SqliteTransaction trans)
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
    
    internal static void PopulateList(ListView list)
    {
      list.Clear();
      list.Columns.Add("Pattern", 100); // Width 100
      list.Columns.Add("Seconds", 80);  // Width 80
      foreach (var p in model.patterns)
      {
        var item = new ListViewItem(p.Key);
        item.SubItems.Add(p.Value.ToString());
        item.SubItems.Add("todo");
        list.Items.Add(item);
      }
    }

    internal static void AddPattern(string text, int seconds)
    {

      if (text.Length > 0)
      {
        model.AddPattern(text, seconds);
        Save();
      }

    }

    internal static void RemovePattern(string text)
    {
      model.patterns.Remove(text);
      Save();
    }

    internal static void UpdateOption(string v, int secs)
    {
      model.config[v] = secs.ToString();
      Save();
    }

    internal static string GetOtherSecs()
    {
      return model.config.GetValueOrDefault("other", "0");
    }

    internal static int GetOtherSecsInt()
    {
      var secs = 0;
      int.TryParse(GetOtherSecs(), out secs);
      return secs;
    }

    internal static Dictionary<string, int> GetPatterns()
    {
      return model.patterns;
    }
  }
}
