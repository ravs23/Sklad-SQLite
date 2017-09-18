using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sklad
{

    static class Category
    {
        public static List<CategoryOne> category;

        public static void MakeList()
        {
            FillDBCategory();
            category = new List<CategoryOne>();

            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string sql = @"SELECT * FROM P_category";

                SQLiteCommand command = new SQLiteCommand(sql, connection);
                
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    int id = reader.GetInt32(0); ;
                    string name = (string)reader[1];
                    category.Add(new CategoryOne() {Id=id, Name=name });
                }

                reader.Close();

            }
        }

        // если БД таблица пустая - записать в неё категории продуктов по-умолчани
        static void FillDBCategory()
        {
            if (!CheckTableCategory())
            {
                using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
                {
                    connection.Open();
                    string expression = @"INSERT INTO P_category
                                (name)
                                VALUES 
                                ('Декоративная косметика'),
                                ('Парфюмы'),
                                ('Wellness'),
                                ('Аксессуары'),
                                ('Уход для мужчин'),
                                ('Уход за телом'),
                                ('Уход за лицом'),
                                ('Уход за волосами'),
                                ('Детская серия')";
                    SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // проверяем БД на наличие в таблице P_category хотя бы одной записи
        static bool CheckTableCategory()
        {
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string expression = @"SELECT COUNT(*) FROM P_category";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                long count = (long)cmd.ExecuteScalar();
                return count != 0;
            }
        }
    }

    struct CategoryOne
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
