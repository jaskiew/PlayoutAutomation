using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Database.SQLite
{
    public class Db
    {
        private readonly string _path;

        internal SQLiteConnection Connection { get; }

        public static Db Current { get; } = new Db(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TVPlaySQLiteBD", "LocalData.db"));

        public Db(string path)
        {
            _path = path;
            var builder = new SQLiteConnectionStringBuilder
            {
                DataSource = path
            };
            Connection = new SQLiteConnection(builder.ToString());
            Connection.Open();
        }

        public bool InitializeDatabase()
        {
            if (Version == 0)
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TAS.Database.SQLite.Schema.sql"))
                using (var reader = new StreamReader(stream))
                    if (ExecuteScript(reader.ReadToEnd())) // Script executed
                    {
                        foreach (var eventType in EventTypes.Default)
                            Insert(eventType);
                    }
                Version = (long)new SQLiteCommand("PRAGMA user_version", Connection).ExecuteScalar();
            }
            return true;
        }
    }
}
