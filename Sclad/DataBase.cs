using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sklad
{
    static class DataBase
    {//
        public static readonly string dbFile = "Sklad.db";
        public static string ConStrDB { get; set; } = @"Data Source=" + dbFile + ";Version=3;Pooling=True;Max Pool Size=100;";

        public static bool CheckExistDB()
        {
            return File.Exists(dbFile);
        }

        public static Task CreateAllTabelsAsync()
        {
            return Task.Factory.StartNew(CreateAllTabels);
        }
        public static void CreateAllTabels()
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConStrDB))
            {
                connection.Open();
                // Создание таблицы Product_category
                string expression = @"CREATE TABLE P_category
                                    (
                                    id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                    name TEXT(25) NOT NULL
                                    )";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                    cmd.ExecuteNonQuery();

                    // Создание таблицы Product
                    expression = @"CREATE TABLE Product
                                    (
                                    code INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                    name  TEXT(80) NOT NULL,
                                    category INTEGER NOT NULL,
									FOREIGN KEY (category) REFERENCES P_category(id)
                                    )";
                    cmd.CommandText = expression;
                    cmd.ExecuteNonQuery();



                    // Создание таблицы Catalog_type
                    expression = @"CREATE TABLE C_type
                                    (
                                    id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                    type TEXT(30) NOT NULL
                                    )";
                    cmd.CommandText = expression;
                    cmd.ExecuteNonQuery();

                    // Создание таблицы Catalog_period_year
                    expression = @"CREATE TABLE C_p_year
                                    (
                                    id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                    year INTEGER NOT NULL
                                    )";
                    cmd.CommandText = expression;
                    cmd.ExecuteNonQuery();

                    // Создание таблицы Catalog_period
                    expression = @"CREATE TABLE C_period
                                    (
                                    id INTEGER NOT NULL PRIMARY KEY ASC,
                                    number INTEGER NOT NULL,
                                    year INTEGER NOT NULL,
									FOREIGN KEY (year) REFERENCES C_p_year(id)
                                    )";
                    cmd.CommandText = expression;
                    cmd.ExecuteNonQuery();

                    // Создание таблицы Catalog
                    expression = @"CREATE TABLE Catalog
                                    (
                                    id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                    period INTEGER NOT NULL,
                                    type INTEGER NOT NULL,
                                    FOREIGN KEY (period) REFERENCES C_period(id),
                                    FOREIGN KEY (type) REFERENCES C_type(id)
                                    )";
                    cmd.CommandText = expression;
                    cmd.ExecuteNonQuery();

                    // Создание таблицы Price
                    expression = @"CREATE TABLE Price
                                    (
                                    id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                    code INTEGER NOT NULL,
                                    pricePC REAL NOT NULL,
                                    priceDC REAL NOT NULL,
                                    catalog INTEGER NOT NULL,
                                    quantity INTEGER NOT NULL,
                                    discont INTEGER NOT NULL,
                                    description TEXT(200),
                                    FOREIGN KEY (code) REFERENCES Product(code),
                                    FOREIGN KEY (catalog) REFERENCES Catalog(id)
                                    )";
                    cmd.CommandText = expression;
                    cmd.ExecuteNonQuery();
            }
        }

        public static void DeleteDB()
        {
            File.Delete(dbFile);
        }

        public static void FillTestData()
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConStrDB))
            {
                connection.Open();
                // Заполнение таблицы Product_category
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
                cmd.Transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

                try
                {
                    cmd.ExecuteNonQuery();

                    //Заполнение таблицы Product
                    expression = @"INSERT INTO Product
                                (code, name, category)
                                VALUES 
                                ('20533','Тушь для ресниц «УЛЬТРАобъем»',1),
                                ('20779','Лак для ногтей «100 % цвета» — Розовый Иней',1),
                                ('23410','Мыло «Инжир и лаванда»',3),
                                ('26261','Лак для ногтей «100 % цвета» — Спелая Слива',1),
                                ('29054','Косметичка',5)";
                    cmd.CommandText = expression;
                    cmd.ExecuteNonQuery();

                    //Заполнение таблицы C_type
                    expression = @"INSERT INTO C_type
                                (type)
                                VALUES 
                                ('Основной'),
                                ('Бизнес Класс'),
                                ('Распродажа'),
                                ('Акционный')";
                    cmd.CommandText = expression;
                    cmd.ExecuteNonQuery();

                    //Заполнение таблицы C_p_type
                    expression = @"INSERT INTO C_p_year
                                (year)
                                VALUES 
                                (2016),
                                (2017),
                                (2018),
                                (2019)";
                    cmd.CommandText = expression;
                    cmd.ExecuteNonQuery();

                    //Заполнение таблицы C_period
                    expression = @"INSERT INTO C_period
                            (number, year)
                            VALUES 
                            (1, 1), 
                            (2, 1), 
                            (3, 1), 
                            (4, 1), 
                            (5, 1), 
                            (6, 1), 
                            (7, 1), 
                            (8, 1), 
                            (9, 1), 
                            (10, 1), 
                            (11, 1), 
                            (12, 1), 
                            (13, 1), 
                            (14, 1), 
                            (15, 1), 
                            (16, 2), 
                            (17, 1), 
                            (1, 2), 
                            (2, 2), 
                            (3, 2)";
                    cmd.CommandText = expression;
                    cmd.ExecuteNonQuery();

                    //Заполнение таблицы Catalog
                    expression = @"INSERT INTO Catalog
                                (period, type)
                                VALUES 
                                (14,1),
                                (14,2),
                                (14,3),
                                (15,1),
                                (15,2),
                                (15,3),
                                (16,1)";
                    cmd.CommandText = expression;
                    cmd.ExecuteNonQuery();

                    //Заполнение таблицы Price
                    expression = @"INSERT INTO Price
                                (code, pricePC, priceDC, catalog, quantity, discont)
                                VALUES 
                                (20779,   31.9,    25.5,    1,   1,   1),
                                (20533,   79.0,    63.18,   1,   4,   1),
                                (23410,   49.0,    39.18,   1,   2,   0),
                                (20779,   50.6,    45.15,   4,   3,   0),
                                (26261,   31.9,    25.5,    1,   1,   1),
                                (29054,   219.0,   175.2,   4,   7,   1),
                                (29054,   285.0,   267.1,   7,   2,   0),
                                (20779,   15.1,    11.5,    6,   3,   1)";
                    cmd.CommandText = expression;
                    cmd.ExecuteNonQuery();

                    cmd.Transaction.Commit();
                }
                catch (Exception e)
                {
                    System.Windows.Forms.MessageBox.Show(e.Message);
                    cmd.Transaction.Rollback();
                }
            }
        }






    }
}

//  Можно использовать в MS SQL Server
//  выбрать все категории, на которые не ссылается ни один продукт (не задействованные категории)
//  SELECT P_category.id
//  FROM Product
//  RIGHT JOIN P_category
//  ON Product.category = P_category.id
//  WHERE Product.category IS NULL

//  Можно использовать и в MS SQL Server и в SQLite
//  SELECT Product_category.id
//  FROM Product_category
//  LEFT JOIN Product
//  ON Product_category.id = Product.category
//  WHERE Product.category IS NULL



 
