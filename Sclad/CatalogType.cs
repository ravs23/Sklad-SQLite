using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sklad
{

    static class CatalogType
    {
        public static List<string> catalogType;

        public static Task MakeListAsync()
        {
            return Task.Factory.StartNew(MakeList);
        }

        public static void MakeList()
        {
            FillDBCatalogType();

            catalogType = new List<string>()
            {
                "Основной",
                "Бизнес Класс",
                "Распродажа",
                "Акционный"
            };
        }

        // если в БД таблица пустая - записать в неё типы каталогов
        static void FillDBCatalogType()
        {
            if (CheckTableCatalogType())
            {
                using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
                {
                    connection.Open();
                    string expression = @"INSERT INTO C_type
                                (type)
                                VALUES 
                                ('Основной'),
                                ('Бизнес Класс'),
                                ('Распродажа'),
                                ('Акционный')";
                    SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // проверяем БД на наличие в таблице C_type записей
        static bool CheckTableCatalogType()
        {
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string expression = @"SELECT COUNT(*) FROM C_type";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                long count = (long)cmd.ExecuteScalar();
                return count == 0;
            }
        }


    }
}