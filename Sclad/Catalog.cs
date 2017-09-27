using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sklad
{
    static class Catalog
    {
        public static List<CatalogOne> catalog;

        public static Task MakeListAsync()
        {
            return Task.Factory.StartNew(MakeList);
        }

        // Выбираем таблицу Catalog и создаем список существующих каталогов и типов
        public static void MakeList()
        {

            catalog = new List<CatalogOne>();

            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                string sql = @"SELECT Catalog.id, Catalog.period, Catalog.type
                               FROM Catalog";
                //

                connection.Open();
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        CatalogOne catalogOne = new CatalogOne();

                        catalogOne.Id = reader.GetInt32(0);
                        catalogOne.Period = reader.GetInt32(1);
                        catalogOne.Type = reader.GetInt32(2);

                        catalog.Add(catalogOne);
                    }
                }

                reader.Close();

            }
        }

        // Искуственный фил во время разработки. В рабочей программе не используется этот метод,
        // так как программа поставляется с пустыми таблицами, заполняемыми в этом методе
        public static void FillDBCatalog()
        {
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();

                //Заполнение таблицы Catalog
                string expression = @"INSERT INTO Catalog
                                    (period, type)
                                    VALUES 
                                    (14,1),
                                    (14,2),
                                    (14,3),
                                    (15,1),
                                    (15,2),
                                    (15,3),
                                    (16,1)";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                cmd.ExecuteNonQuery();
            }
        }
    }


    struct CatalogOne
    {
        public int Id { get; set; }
        public int Period { get; set; }
        public int Type { get; set; }
    }
}
