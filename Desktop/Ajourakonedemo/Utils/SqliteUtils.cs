using System;
using System.Configuration;
using System.IO;
using ArcGISRuntime.Samples.DesktopViewer.Model;
using ArcGISRuntime.Samples.DesktopViewer.SQLite;
using Catel.Logging;
using Community.CsharpSqlite;

namespace ArcGISRuntime.Samples.DesktopViewer.Utils
{
    public class SqliteUtils
    {
        private static readonly SqliteUtils _instance = new SqliteUtils();
        private static readonly string DbPath = ConfigurationManager.AppSettings.Get("SQLite");
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public SqliteUtils()
        {

        }

        public static SqliteUtils Instance
        {
            get { return _instance; }
        }


        public SQLiteConnection GetSQLiteConnection()
        {
            // Kutsutaan joka kerta, kun kantaa käytetään. 
            // Varmistaa, että kaikki taulut on olemassa.
            CreateSqliteDatabase();

            return new SQLiteConnection(DbPath);
        }

        public void CreateSqliteDatabase()
        {
            using (var db = new SQLiteConnection(DbPath))
            {
                Log.Info("Creating new SQLite tables in {0}", DbPath);
                //TODO karsi pois turhat columnit

                if (!HasTable<OptimizationRunModel>(db))
                    db.CreateTable<OptimizationRunModel>();

            }
        }

        /// <summary>
        /// Tarkistaa onko annetussa sqlite-tietokannassa T-luokan taulua
        /// </summary>
        /// <typeparam name="T">Luokka/Model, jolle etsitään taulua</typeparam>
        /// <param name="db">Tietokanta, josta etsitään</param>
        /// <returns></returns>
        public bool HasTable<T>(SQLiteConnection db) where T : new()
        {
            var check = db.GetTableInfo(typeof(T).Name);
            if (check == null || check.Count == 0)
            {
                Log.Warning("Table for model {0} was not found in database: {1}", typeof(T).Name, DbPath);
                return false;
            }
            return true;
        }

        public void DeleteDatabase()
        {
            try
            {
                Log.Info("Deleting SQLite database in {0}", DbPath);
                File.Delete(DbPath);
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("Unable to delete {0}", DbPath), ex);
            }

        }

        public void Insert<T>(T model) where T : new()
        {
            try
            {
                using (var db = Instance.GetSQLiteConnection())
                {
                    db.Insert(model);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.Message.Contains("has no column"))
                {
                    LuoTauluUudeelleen<T>();
                    Insert(model);
                }
                else
                {
                    throw;
                }

            }
        }

        private void LuoTauluUudeelleen<T>() where T : new()
        {
            using (var db = new SQLiteConnection(DbPath))
            {
                Log.Info("Creating new SQLite tables in {0}", DbPath);
                //TODO karsi pois turhat columnit

                if (HasTable<T>(db))
                {
                    db.DropTable<T>();
                    db.CreateTable<T>();
                }



            }
        }
    }
}
