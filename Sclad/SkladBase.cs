using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sklad
{


    static class SkladBase
    {
        /// <summary>
        /// Выбираем продукты из БД по коду для заполнения основного грида
        /// </summary>
        /// <param name="code">Код продутка</param>
        /// <returns></returns>
        public static DataTable SearchProdByCode(string code)
        {
            DataTable result = new DataTable();

            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string sql = @"SELECT Product.code, Product.name, sum(Price.quantity) AS total
                            FROM Product, Price
                            WHERE Price.code = Product.code AND Product.code LIKE @code
                            GROUP BY Product.code, Product.Name";

                SQLiteCommand command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("code", code + "%");
                SQLiteDataReader reader = command.ExecuteReader();

                result.Load(reader);
                reader.Close();
            }

            result.Columns[2].ReadOnly = false;
            return result;
        }

        /// <summary>
        /// Выбираем продукты из БД по названию для заполнения основного грида
        /// </summary>
        /// <param name="name">Название продукта</param>
        /// <returns></returns>
        public static DataTable SearchProdByName(string name)
        {
            DataTable result = new DataTable();

            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string sql = @"SELECT Product.code, Product.name, sum(Price.quantity) AS total
                            FROM Product, Price
                            WHERE Price.code = Product.code AND Product.Name LIKE @name
                            GROUP BY Product.code, Product.Name";

                SQLiteCommand command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("name", "%" + name + "%");
                SQLiteDataReader reader = command.ExecuteReader();

                result.Load(reader);
                reader.Close();

            }
            result.Columns[2].ReadOnly = false;
            return result;
        }

        /// <summary>
        /// Заполняем грид Details в соответствии с выбранным продуктом в основном гриде
        /// </summary>
        /// <param name="currentCode">Код выбранного продукта в основном гриде</param>
        /// <returns></returns>
        public static DataTable FilldgvDetails(string currentCode)
        {
            DataTable resultDetails = new DataTable();

            if (String.IsNullOrEmpty(currentCode))
                return resultDetails;

            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string sql = @"SELECT Price.code, Price.priceDC, Price.pricePC, Price.quantity, C_period.number, C_p_year.year, Price.discont, Price.description, C_type.type
                            FROM Price, C_period, C_type, Catalog, C_p_year
                            WHERE Price.catalog = Catalog.id AND Catalog.type = C_type.id AND Catalog.period = C_period.id and C_period.year = C_p_year.id AND Price.code = @code";

                SQLiteCommand command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("code", currentCode);
                SQLiteDataReader reader = command.ExecuteReader();

                resultDetails.Load(reader);
                reader.Close();

            }
            return resultDetails;
        }




        /// <summary>
        /// Изменяем количество продукта в БД
        /// </summary>
        /// <param name="code">Код</param>
        /// <param name="quant">Количество</param>
        /// <param name="dc">ДЦ</param>
        /// <param name="pc">ПЦ</param>
        /// <param name="discont">Дисконт</param>
        /// <param name="upDownOp">Отнимаем (Down) или прибавляем (Up)</param>
        public static void UpDownQtyPrice(int code, int quant, double dc, double pc, bool discont, UpDownOperation upDownOp)
        {
            int new_quant = quant;

            if (upDownOp == UpDownOperation.Down)  // определяем что нужно: отнять или прибавить кол-во
                new_quant--;
            else
                new_quant++;

            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string sql = @"UPDATE Price 
                               SET quantity = @new_quant
                               WHERE code = @code AND quantity = @quant AND priceDC = @dc AND pricePC = @pc AND discont = @discont";

                SQLiteCommand command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("code", code);
                command.Parameters.AddWithValue("quant", quant);
                command.Parameters.AddWithValue("dc", dc);
                command.Parameters.AddWithValue("pc", pc);
                command.Parameters.AddWithValue("discont", discont);
                command.Parameters.AddWithValue("new_quant", new_quant);
                command.ExecuteNonQuery();
            }
        }
        /// <summary>
        /// Изменяем количество продукта в БД асинхронно
        /// </summary>
        /// <param name="code">Код</param>
        /// <param name="quant">Количество</param>
        /// <param name="dc">ДЦ</param>
        /// <param name="pc">ПЦ</param>
        /// <param name="discont">Дисконт</param>
        /// <param name="upDownOp">Отнимаем (Down) или прибавляем (Up)</param>
        public static Task UpDownQtyPriceAsync(int code, int quant, double dc, double pc, bool discont, UpDownOperation upDownOp)
        {
            Params p = new Params();
            p.code = code;
            p.quant = quant;
            p.dc = dc;
            p.pc = pc;
            p.discont = discont;
            p.upDownOp = upDownOp;
            return Task.Factory.StartNew(UpDownQtyPrice, p);
        }
        static void UpDownQtyPrice(object param)
        {
            Params p = (Params)param;
            UpDownQtyPrice(p.code, p.quant, p.dc, p.pc, p.discont, p.upDownOp);
        }


        /// <summary>
        /// Удаляем продукт из БД, из таблицы Price
        /// </summary>
        /// <param name="code">Код</param>
        /// <param name="quant">Количество</param>
        /// <param name="dc">ДЦ</param>
        /// <param name="pc">ПЦ</param>
        /// <param name="discont">Дисконт</param>
        /// <summary>
        public static void DeleteProdFromPrice(int code, int quant, double dc, double pc, bool discont)
        {
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string sql = @"DELETE FROM Price WHERE code = @code AND quantity = @quant AND priceDC = @dc AND pricePC = @pc AND discont = @discont";

                SQLiteCommand command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("code", code);
                command.Parameters.AddWithValue("quant", quant);
                command.Parameters.AddWithValue("dc", dc);
                command.Parameters.AddWithValue("pc", pc);
                command.Parameters.AddWithValue("discont", discont);
                command.ExecuteNonQuery();
            }
        }
        /// Удаляем продукт из БД, из таблицы Price асинхронно
        /// </summary>
        /// <param name="code">Код</param>
        /// <param name="quant">Количество</param>
        /// <param name="dc">ДЦ</param>
        /// <param name="pc">ПЦ</param>
        /// <param name="discont">Дисконт</param>
        public static Task DeleteProdFromPriceAsync(int code, int quant, double dc, double pc, bool discont)
        {
            Params p = new Params();
            p.code = code;
            p.quant = quant;
            p.dc = dc;
            p.pc = pc;
            p.discont = discont;

            return Task.Factory.StartNew(DeleteProdFromPrice, p);
        }

        /// <summary>
        /// Удаляем все продукты из БД, из таблицы Price по коду
        /// </summary>
        /// <param name="code">Код</param>
        public static void DeleteProdFromPrice(int code)
        {
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string sql = @"DELETE FROM Price 
                               WHERE code = @code";

                SQLiteCommand command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("code", (int)code);
                command.ExecuteNonQuery();
            }
        }
        /// <summary>
        /// Удаляем все продукты из БД, из таблицы Price по коду асинхронно
        /// </summary>
        /// <param name="code">Код</param>
        public static Task DeleteProdFromPriceAsync(int code)
        {
            return Task.Factory.StartNew(DeleteProdFromPrice, code);
        }

        static void DeleteProdFromPrice(object param)
        {
            if (param is int)
                DeleteProdFromPrice((int)param);
            else
            {
                Params p = (Params)param;
                DeleteProdFromPrice(p.code, p.quant, p.dc, p.pc, p.discont);
            }
        }


        /// <summary>
        /// Удаляем продукт из БД, из таблицы Product.
        /// NOTE! Удалить можно только после удаления всех продуктов с таким же кодом из таблицы Price
        /// </summary>
        /// <param name="code">Код</param>
        public static void DeleteProdFromProductTable(int code)
        {
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string sql = @"DELETE FROM Product
                               WHERE code = @code";

                SQLiteCommand command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("code", (int)code);
                command.ExecuteNonQuery();
            }
        }
        /// <summary>
        /// Удаляем продукт из БД, из таблицы Product. Асинхронно
        /// NOTE! Удалить можно только после удаления всех продуктов с таким же кодом из таблицы Price
        /// </summary>
        /// <param name="code">Код</param>
        public static Task DeleteProdFromProductTableAsync(int code)
        {
            return Task.Factory.StartNew(DeleteProdFromProductTable, code);
        }
        static void DeleteProdFromProductTable(object code)
        {
            DeleteProdFromProductTable((int)code);
        }


        /// <summary>
        /// Добавляем кталог в таблицу Catalog (id периода и тип)
        /// </summary>
        /// <param name="period"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int AddCatalog(int period, int type)
        {
            // Проверяем есть ли такой период и тип в БД. Если есть, получаем id (табл Catalog), если нет - создаём и получаем id
            int catalogIndex;
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string expression = @"SELECT id
                                    FROM  Catalog
                                    WHERE period = @period AND type = @type";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                cmd.Parameters.AddWithValue("period", period);
                cmd.Parameters.AddWithValue("type", type);
                SQLiteDataReader reader = cmd.ExecuteReader();

                if (!reader.HasRows) // если возвращается 0 - такого периода+типа нет в БД
                {
                    catalogIndex = 0;
                }
                else
                {
                    reader.Read();
                    catalogIndex = reader.GetInt32(0);
                    reader.Close();
                }
            }

            // если такого периода+типа нету в БД, создаём и получаем Id
            if (catalogIndex == 0)
            {
                using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
                {
                    connection.Open();
                    string expression = @"INSERT INTO Catalog
                                        (period, type)
                                        VALUES 
                                        (@period, @type)";
                    SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                    cmd.Parameters.AddWithValue("period", period);
                    cmd.Parameters.AddWithValue("type", type);
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "SELECT last_insert_rowid()";

                    catalogIndex = Convert.ToInt32(cmd.ExecuteScalar());

                }
            }
            Catalog.MakeList();

            return catalogIndex;
        }
        /// <summary>
        /// Добавляем кталог в таблицу Catalog (id периода и тип) асинхронно
        /// </summary>
        /// <param name="period"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Task<int> AddCatalogAsync(int period, int type)
        {
            Params p = new Params();
            p.period = period;
            p.type = type;

            return Task<int>.Factory.StartNew(AddCatalog, p);
        }
        static int AddCatalog(object param)
        {
            Params p = (Params)param;
            return AddCatalog(p.period, p.type);
        }
        

        #region Добавляем каталожный период
        /// <summary>
        /// Добавляем каталожный период в БД (без проверки на его существование)
        /// </summary>
        /// <param name="period"></param>
        /// <param name="year"></param>
        public static void AddCatalogPeriod(int period, int year)
        {

            int yearId = CheckExistYear(year);

            if (yearId == 0)
            {
                yearId = InsertYear(year);
            }

            InsertPeriod(period, yearId);

            //обновляем информацию в локальном листе
            CatalogPeriod.MakeList();
        }
        /// <summary>
        /// Добавляем каталожный период в БД асинхронно (без проверки на его существование)
        /// </summary>
        /// <param name="period"></param>
        /// <param name="year"></param>
        public static Task AddCatalogPeriodAsync(int period, int year)
        {
            Params p = new Params();
            p.period = period;
            p.year = year;

            return Task.Factory.StartNew(AddCatalogPeriod, p);
        }
        static void AddCatalogPeriod(object param)
        {
            Params p = (Params)param;
            AddCatalogPeriod(p.period, p.year);
        }

        static int CheckExistYear(int year)
        {
            int yearIndex;
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string expression = @"SELECT id
                                FROM  C_p_year
                                WHERE year = @year";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                cmd.Parameters.AddWithValue("year", year);
                SQLiteDataReader reader = cmd.ExecuteReader();

                if (!reader.HasRows) // если возвращается 0 - такого года нет в БД
                    return 0;

                reader.Read();
                yearIndex = reader.GetInt32(0);
                reader.Close();
            }

            return yearIndex;
        }

        static int InsertYear(int year)
        {
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string expression = @"INSERT INTO C_p_year
                                (year)
                                VALUES 
                                (@year)";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                cmd.Parameters.AddWithValue("year", year);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "SELECT last_insert_rowid()";

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        static void InsertPeriod(int period, int yearId)
        {
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();

                string expression = @"INSERT INTO C_period
                                    (number, year)
                                    VALUES
                                    (@period, @yearId)";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                cmd.Parameters.AddWithValue("yearId", yearId);
                cmd.Parameters.AddWithValue("period", period);
                cmd.ExecuteNonQuery();
            }
        }
        #endregion






        /// <summary>
        /// Проверяем есть ли такой продукт в БД (табл Product). Еесли нет - создаём
        /// </summary>
        /// <param name="code">Код продукта</param>
        public static void AddProduct(string code, string name, int category)
        {
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string expression = @"SELECT COUNT(code)
                                    FROM  Product
                                    WHERE code = @code";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                cmd.Parameters.AddWithValue("code", code);
                long existCode = (long)cmd.ExecuteScalar();

                if (existCode == 0) // если возвращается 0 - такого продукта нет в БД (табл Product)
                {
                    expression = @"INSERT INTO Product
                                        (code, name, category)
                                        VALUES 
                                        (@code, @name, @category);";
                    cmd.CommandText = expression;
                    cmd.Parameters.AddWithValue("name", name);
                    cmd.Parameters.AddWithValue("category", category);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Добавляем продукт в таблицу Price
        /// </summary>
        /// <param name="code"></param>
        /// <param name="pricePC"></param>
        /// <param name="priceDC"></param>
        /// <param name="catalogId"></param>
        /// <param name="quantity"></param>
        /// <param name="discont"></param>
        /// <param name="description"></param>
        public static void AddProductToPrice(string code, double pricePC, double priceDC, int catalogId, int quantity, bool discont, string description)
        {
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string expression = @"INSERT INTO Price
                                (code, pricePC, priceDC, catalog, quantity, discont, description)
                                VALUES 
                                (@code, CAST(@pricePC AS money), CAST(@priceDC AS money), @catalog, @quantity, @discont, @description);";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                cmd.Parameters.AddWithValue("code", code);
                cmd.Parameters.AddWithValue("pricePC", pricePC);
                cmd.Parameters.AddWithValue("priceDC", priceDC);
                cmd.Parameters.AddWithValue("catalog", catalogId);
                cmd.Parameters.AddWithValue("quantity", quantity);
                cmd.Parameters.AddWithValue("discont", discont ? 1 : 0);
                cmd.Parameters.AddWithValue("description", description);

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Добавление категории продукта
        /// </summary>
        /// <param name="categoryName">Название категории</param>
        public static void AddCategory(string categoryName)
        {
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string expression = @"INSERT INTO P_category
                                (name)
                                VALUES 
                                (@cat_name)";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                cmd.Parameters.AddWithValue("cat_name", categoryName);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Поиск продукта в БД для отображения в форме Результат поиска
        /// </summary>
        /// <param name="fieldSearch">Искомое значение</param>
        /// <param name="searchBy">По какому полю искать</param>
        /// <returns></returns>
        public static DataTable SearchForAdd(string fieldSearch, SearchBy searchBy)
        {
            DataTable ResultSearchDB = new DataTable();

            if (fieldSearch == string.Empty)
                return null;

            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                SQLiteCommand command = connection.CreateCommand();

                string sql = string.Empty;

                switch (searchBy)
                {
                    case SearchBy.Code:
                        sql = @"SELECT Product.code, Product.name, Price.priceDC, Price.pricePC, Price.discont, C_p_year.year || ' / ' || C_period.number AS period
                               FROM Product, Price, C_period, C_p_year, Catalog
                               WHERE Product.code = Price.code AND Price.catalog = Catalog.id AND Catalog.period = C_period.id AND C_period.year = C_p_year.id AND Product.code = @code";
                        command.Parameters.AddWithValue("code", fieldSearch);
                        command.CommandText = sql;
                        break;

                    case SearchBy.Name:
                        sql = @"SELECT Product.code, Product.name, Price.priceDC, Price.pricePC, Price.discont, C_p_year.year || ' / ' || C_period.number AS period
                               FROM Product, Price, C_period, C_p_year, Catalog
                               WHERE Product.code = Price.code AND Price.catalog = Catalog.id AND Catalog.period = C_period.id AND C_period.year = C_p_year.id AND Product.name LIKE @name";
                        command.Parameters.AddWithValue("name", "%" + fieldSearch + "%");
                        command.CommandText = sql;
                        break;
                }

                SQLiteDataReader reader = command.ExecuteReader();

                ResultSearchDB.Load(reader);
                reader.Close();
            }

            // Меняем все периоды XXXX / X на XXXX / 0X
            ResultSearchDB.Columns["period"].ReadOnly = false;
            foreach (DataRow row in ResultSearchDB.Rows)
            {
                if ((row["period"].ToString()).Length == 8)
                {
                    row["period"] = row["period"].ToString().Insert(7, "0");
                }
            }
            ResultSearchDB.Columns["period"].ReadOnly = true;

            return ResultSearchDB;
        }

        public static int CheckExistProductFull(out int quantityBD, string code, double pricePC, double priceDC, int catalogId, bool discont)
        {
            int productIndex;
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string expression = @"SELECT Price.id, Price.quantity
                                    FROM Price
                                    WHERE Price.code = @code AND
                                    Price.priceDC = @priceDC AND Price.pricePC = @pricePC AND
                                    Price.catalog = @catalog AND Price.discont = @discont";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                cmd.Parameters.AddWithValue("code", code);
                cmd.Parameters.AddWithValue("priceDC", priceDC);
                cmd.Parameters.AddWithValue("pricePC", pricePC);
                cmd.Parameters.AddWithValue("catalog", catalogId);
                cmd.Parameters.AddWithValue("discont", discont);
                SQLiteDataReader reader = cmd.ExecuteReader();

                if (!reader.HasRows) // если возвращается 0 - такого года нет в БД
                {
                    quantityBD = 0;
                    return 0;
                }
                reader.Read();
                productIndex = reader.GetInt32(0);
                quantityBD = reader.GetInt32(1);
                reader.Close();
            }
            return productIndex;
        }

        public static void UpdateProductQuantInPrice(int id, int newQuantity)
        {
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string sql = @"UPDATE Price 
                               SET quantity = @newQuantity
                               WHERE id = @id";

                SQLiteCommand command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("newQuantity", newQuantity);
                command.ExecuteNonQuery();
            }
        }


        /// <summary>
        /// Преобразуем числовой формат "Период" и "Год" в текстовый "Период / Год"
        /// </summary>
        /// <param name="period"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        public static string ConvertPeriodToText(int period, int year)
        {
            string num = period.ToString();

            // если в настройках стоит "показывать с нулём", добавляем 0 перед каталогом
            if (Settings.DisplayCatalogPeriodsWithZero)
            {
                if (num.Length == 1)
                    num = num.Insert(0, "0");
            }

            return num + " / " + year.ToString();
        }

        #region Optimization

        public static void Optimization()
        {
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string expression = @"
                                    SELECT Catalog.id
                                    FROM Catalog
                                    LEFT JOIN Price
                                    ON Catalog.id = Price.catalog
                                    WHERE Price.catalog IS NULL
                                    ;
                                    SELECT C_period.id
                                    FROM C_period
                                    LEFT JOIN Catalog
                                    ON C_period.id = Catalog.period
                                    WHERE Catalog.period IS NULL
                                    ;
                                    SELECT P_category.id
                                    FROM P_category
                                    LEFT JOIN Product
                                    ON P_category.id = Product.category
                                    WHERE Product.category IS NULL
                                    ;
                                    SELECT C_p_year.id
                                    FROM C_p_year
                                    LEFT JOIN C_period
                                    ON C_p_year.id = C_period.year
                                    WHERE C_period.year IS NULL";

                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                SQLiteDataReader reader = cmd.ExecuteReader();

                SQLiteCommand cmdDel = new SQLiteCommand(connection);

                // Обрабатываем первый запрос
                if (reader.HasRows)
                {
                    //MessageBox.Show("1th");
                    while (reader.Read())
                    {
                        expression = @"DELETE FROM Catalog WHERE id = @id";
                        cmdDel.CommandText = expression;
                        cmdDel.Parameters.Clear();
                        cmdDel.Parameters.AddWithValue("id", reader.GetInt32(0));
                        cmdDel.ExecuteNonQuery();
                    }
                }

                // Обрабатываем второй запрос
                reader.NextResult();

                if (reader.HasRows)
                {
                    //MessageBox.Show("2th");
                    while (reader.Read())
                    {
                        expression = @"DELETE FROM C_period WHERE id = @id";
                        cmdDel.CommandText = expression;
                        cmdDel.Parameters.Clear();
                        cmdDel.Parameters.AddWithValue("id", reader.GetInt32(0));
                        cmdDel.ExecuteNonQuery();
                    }
                }

                // Обрабатываем третий запрос
                reader.NextResult();

                if (reader.HasRows)
                {
                    //MessageBox.Show("3th");
                    while (reader.Read())
                    {
                        expression = @"DELETE FROM P_category WHERE id = @id";
                        cmdDel.CommandText = expression;
                        cmdDel.Parameters.Clear();
                        cmdDel.Parameters.AddWithValue("id", reader.GetInt32(0));
                        cmdDel.ExecuteNonQuery();
                    }
                }

                // Обрабатываем четвертый запрос
                reader.NextResult();

                if (reader.HasRows)
                {
                    //MessageBox.Show("4th");
                    while (reader.Read())
                    {
                        expression = @"DELETE FROM C_p_year WHERE id = @id";
                        cmdDel.CommandText = expression;
                        cmdDel.Parameters.Clear();
                        cmdDel.Parameters.AddWithValue("id", reader.GetInt32(0));
                        cmdDel.ExecuteNonQuery();
                    }
                }
                reader.Close();
            }
        }
        #endregion
    }
}