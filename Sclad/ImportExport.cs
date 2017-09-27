using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using OfficeOpenXml;
using System.IO;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace Sklad
{
    static class ImportExport
    {
        static int processedProduct;
        static int allProduct;
        static int errorCount;

        #region Export

        public static void Export(string fileName)
        {
            DataTable dataDB = GetDataFromDB();

            using (var excel = new ExcelPackage())
            {
                ExcelWorksheet ws = excel.Workbook.Worksheets.Add("Sklad " + DateTime.Now.Date.ToShortDateString());

                ws.Cells[1, 1].Value = "Код";
                ws.Cells[1, 2].Value = "Наименование";
                ws.Cells[1, 3].Value = "ДЦ";
                ws.Cells[1, 4].Value = "ПЦ";
                ws.Cells[1, 5].Value = "Количество";
                ws.Cells[1, 6].Value = "Скидка";
                ws.Cells[1, 7].Value = "Период";
                ws.Cells[1, 8].Value = "Год";
                ws.Cells[1, 9].Value = "Каталог";
                ws.Cells[1, 10].Value = "Категория";
                ws.Cells[1, 11].Value = "Описание";

                int rowNum = 2;
                foreach (DataRow row in dataDB.Rows)
                {
                    int colNum = 1;
                    foreach (var column in row.ItemArray)
                    {
                        if (colNum == 6)
                            ws.Cells[rowNum, colNum].Value = Convert.ToBoolean(column) ? "да" : "нет";
                        else
                            ws.Cells[rowNum, colNum].Value = column;
                        colNum++;
                    }
                    rowNum++;
                }
                ws.Cells[1, 1, ws.Dimension.End.Row, ws.Dimension.End.Column].AutoFitColumns();
                ws.Cells[1, 1, 1, ws.Dimension.End.Column].Style.Font.Bold = true;
                ws.Cells[1, 1, 1, ws.Dimension.End.Column].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                ws.Cells[2, 1, ws.Dimension.End.Row, ws.Dimension.End.Column].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;

                try
                {
                    excel.SaveAs(new FileInfo(fileName));
                    MessageBox.Show("Экспортировано: " + (rowNum - 2) + " записей", "Экспорт выполнен успешно",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (InvalidOperationException e)
                {
                    MessageBox.Show(e.Message + Environment.NewLine +
                            "Данный файл возможно открыт в другом прилоении либо диск больше недоступен.");
                }
            }
        }
        public static Task ExportAsync(string fileName)
        {
            return Task.Factory.StartNew(Export, fileName);
        }
        static void Export(object param)
        {
            Export((string)param);
        }


        static DataTable GetDataFromDB()
        {
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string expression = @"SELECT Product.code, Product.name, Price.priceDC, Price.pricePC, Price.quantity, Price.discont, C_period.number, C_p_year.year, C_type.type, P_category.name, Price.description
                                    FROM Product, Price, P_category, Catalog, C_period, C_p_year, C_type
                                    WHERE Product.code = Price.code AND Product.category = P_category.id AND Price.catalog = Catalog.id AND Catalog.period = C_period.id AND C_period.year = C_p_year.id AND Catalog.type = C_type.id
                                    ORDER BY Product.code, C_p_year.year, C_period.number";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                SQLiteDataReader reader = cmd.ExecuteReader();

                DataTable dt = new DataTable();
                dt.Load(reader);
                reader.Close();

                return dt;
            }
        }

        #endregion

        #region Import

        public static void Import(string fileName)
        {
            try
            {
                DataTable dt = GetDataFromXLSX(fileName);

                foreach (DataRow row in dt.Rows)
                {
                    int yearID = GetIdYear(row["year"].ToString());
                    int catalogPeriodID = GetCatalogPeriodID(row["number"].ToString(), yearID);
                    int catalogTypeID = GetCatalogTypeID(row["type"].ToString());
                    int catalogID = GetCatalogID(catalogPeriodID, catalogTypeID);
                    int categoryID = GetCategoryID(row["category"].ToString());
                    bool existProductInProductTable = ExistProductInProductTable(row["code"].ToString());
                    if (!existProductInProductTable)
                    {
                        AddProductToProductTable(row["code"].ToString(), row["name"].ToString(), categoryID);
                    }
                    int quantityDB;
                    int productIDInPriceTable = ExistSuchProductInPriceTable(out quantityDB, row["code"].ToString(), Convert.ToDouble(row["priceDC"]), Convert.ToDouble(row["pricePC"]), catalogID, Convert.ToBoolean(row["discont"]));
                    if (productIDInPriceTable == 0)
                        AddProductToPriceTable(row["code"].ToString(), Convert.ToDouble(row["priceDC"]), Convert.ToDouble(row["pricePC"]), catalogID, row["quantity"].ToString(), Convert.ToBoolean(row["discont"]), row["description"].ToString());
                    else
                        UpdateProductInPriceTable(productIDInPriceTable, quantityDB + Convert.ToInt32(row["quantity"]));

                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                MessageBox.Show($"Добавленно: {processedProduct} из {allProduct} записей" + Environment.NewLine +
                    $"Пропущенно {errorCount} записей",
                    (processedProduct != 0) ? "Импорт выполнен успешно" : "Импорт не выполнен", MessageBoxButtons.OK,
                    (processedProduct != 0) ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            }
        }

        public static Task ImportAsync(string fileName)
        {
            return Task.Factory.StartNew(Import, fileName);
        }
        static void Import(object fileName)
        {
            Import((string)fileName);
        }

        static DataTable GetDataFromXLSX(string fileName)
        {
            processedProduct = 0;
            allProduct = 0;
            errorCount = 0;

            DataTable dt = CreateDataTable();

            try
            {
                using (ExcelPackage excel = new ExcelPackage(new FileInfo(fileName)))
                {
                    int startRow;
                    int startCol;
                    if (!ExcelSearch.SearchStartCell(excel, out startRow, out startCol))
                    {
                        MessageBox.Show("В файле \"" + fileName + "\" не найдены данные");
                        return dt;
                    }

                    ExcelWorksheet ws = excel.Workbook.Worksheets[1];
                    int totalRow = ws.Dimension.End.Row;

                    for (int i = startRow; i <= totalRow; i++)
                    {
                        DataRow row = dt.NewRow();

                        //Проверяем на валидность все поля в XLSX и добавляем в DataTable
                        // code
                        int code;
                        if (ws.Cells[i, 1].Value == null || !int.TryParse(ws.Cells[i, 1].Text, out code))
                        {
                            errorCount++;
                            continue;
                        }
                        else
                        {
                            row[0] = code;
                        }

                        // name
                        if (ws.Cells[i, 2].Value == null || ws.Cells[i, 2].Text.Length == 0)
                        {
                            errorCount++;
                            continue;
                        }
                        else
                        {
                            if (ws.Cells[i, 2].Text.Length > 80)
                                row[1] = ws.Cells[i, 2].Text.Substring(0, 80);
                            else row[1] = ws.Cells[i, 2].Text;
                        }

                        // priceDC
                        double priceDC;
                        if (ws.Cells[i, 3].Value == null || !double.TryParse(ws.Cells[i, 3].Text.Replace('.', ','), out priceDC))
                        {
                            errorCount++;
                            continue;
                        }
                        else
                        {
                            row[2] = priceDC;
                        }

                        // pricePC
                        double pricePC;
                        if (ws.Cells[i, 4].Value == null || !double.TryParse(ws.Cells[i, 4].Text.Replace('.', ','), out pricePC))
                        {
                            errorCount++;
                            continue;
                        }
                        else
                        {
                            row[3] = pricePC;
                        }

                        // Quantity
                        int quantity;
                        if (ws.Cells[i, 5].Value == null || !int.TryParse(ws.Cells[i, 5].Text, out quantity))
                        {
                            errorCount++;
                            continue;
                        }
                        else
                        {
                            row[4] = quantity;
                        }

                        // Discont
                        if (ws.Cells[i, 6].Value == null || ws.Cells[i, 6].Text.ToLower() != "да" & ws.Cells[i, 6].Text.ToLower() != "нет")
                        {
                            errorCount++;
                            continue;
                        }
                        else
                        {
                            if (ws.Cells[i, 6].Text.ToLower() == "да")
                                row[5] = true;
                            else
                                row[5] = false;
                        }

                        // Period
                        int period;
                        if (ws.Cells[i, 7].Value == null || !int.TryParse(ws.Cells[i, 7].Text, out period))
                        {
                            errorCount++;
                            continue;
                        }
                        else
                        {
                            row[6] = period;
                        }

                        // Year
                        int year;
                        if (ws.Cells[i, 8].Value == null || !int.TryParse(ws.Cells[i, 8].Text, out year))
                        {
                            errorCount++;
                            continue;
                        }
                        else
                        {
                            row[7] = year;
                        }

                        // type of catalog
                        if (ws.Cells[i, 9].Value == null || ws.Cells[i, 9].Text.Length == 0)
                        {
                            errorCount++;
                            continue;
                        }
                        else
                        {
                            if (ws.Cells[i, 9].Text.Length > 30)
                                row[8] = ws.Cells[i, 9].Text.Substring(0, 30);
                            else row[8] = ws.Cells[i, 9].Text;
                        }

                        // category
                        if (ws.Cells[i, 10].Value == null || ws.Cells[i, 10].Text.Length == 0)
                        {
                            errorCount++;
                            continue;
                        }
                        else
                        {
                            if (ws.Cells[i, 10].Text.Length > 25)
                                row[9] = ws.Cells[i, 10].Text.Substring(0, 25);
                            else row[9] = ws.Cells[i, 10].Text;
                        }

                        // description
                        if (ws.Cells[i, 11].Text.Length > 200)
                        {
                            row[10] = ws.Cells[i, 11].Text.Substring(0, 200);
                        }
                        else
                            row[10] = ws.Cells[i, 11].Text;



                        // если всё ок, то добавляем строку
                        dt.Rows.Add(row);


                    }
                    processedProduct = totalRow - startRow - errorCount + 1;
                    allProduct = totalRow - startRow + 1;

                    //MessageBox.Show($"Добавленно: {totalRow - startRow - errorCount + 1} из {totalRow - startRow + 1} записей" + Environment.NewLine +
                    //    $"Пропущенно {errorCount} записей",
                    //    (totalRow - startRow - errorCount + 1 != 0) ? "Импорт выполнен успешно" : "Импорт не выполнен", MessageBoxButtons.OK,
                    //    (totalRow - startRow - errorCount + 1 != 0) ? MessageBoxIcon.Information : MessageBoxIcon.Error);

                    //Form1 f1 = new Form1();
                    //f1.dataGridView1.DataSource = dt;
                    //f1.ShowDialog(this);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            return dt;
        }

        static DataTable CreateDataTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("code");
            dt.Columns.Add("name");
            dt.Columns.Add("priceDC");
            dt.Columns.Add("pricePC");
            dt.Columns.Add("quantity");
            dt.Columns.Add("discont");
            dt.Columns.Add("number");
            dt.Columns.Add("year");
            dt.Columns.Add("type");
            dt.Columns.Add("category");
            dt.Columns.Add("description");
            return dt;
        }

        static int GetIdYear(string year)
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
                    yearIndex = 0;
                else
                {
                    reader.Read();
                    yearIndex = reader.GetInt32(0);
                    reader.Close();
                }
            }

            // если такого года нету в БД, создаём и получаем Id
            if (yearIndex == 0)
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

                    yearIndex = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            return yearIndex;
        }

        static int GetCatalogPeriodID(string number, int year)
        {
            int CatalogPeriodIndex;
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string expression = @"SELECT id
                                    FROM  C_period
                                    WHERE number = @number AND year = @year";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                cmd.Parameters.AddWithValue("year", year);
                cmd.Parameters.AddWithValue("number", number);
                SQLiteDataReader reader = cmd.ExecuteReader();

                if (!reader.HasRows) // если возвращается 0 - такого периода нет в БД
                    CatalogPeriodIndex = 0;
                else
                {
                    reader.Read();
                    CatalogPeriodIndex = reader.GetInt32(0);
                    reader.Close();
                }
            }

            // если такого периода нету в БД, создаём и получаем Id
            if (CatalogPeriodIndex == 0)
            {
                using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
                {
                    connection.Open();
                    string expression = @"INSERT INTO C_period
                                        (number, year)
                                        VALUES 
                                        (@number, @year)";
                    SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                    cmd.Parameters.AddWithValue("number", number);
                    cmd.Parameters.AddWithValue("year", year);
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "SELECT last_insert_rowid()";

                    CatalogPeriodIndex = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            return CatalogPeriodIndex;
        }

        static int GetCatalogTypeID(string type)
        {
            int CatalogTypeIndex;
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string expression = @"SELECT id
                                    FROM  C_type
                                    WHERE type = @type";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                cmd.Parameters.AddWithValue("type", type);
                SQLiteDataReader reader = cmd.ExecuteReader();

                if (!reader.HasRows) // если возвращается 0 - такого типа нет в БД
                    CatalogTypeIndex = 0;
                else
                {
                    reader.Read();
                    CatalogTypeIndex = reader.GetInt32(0);
                    reader.Close();
                }
            }

            // если такого типа нету в БД, создаём и получаем Id
            if (CatalogTypeIndex == 0)
            {
                using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
                {
                    connection.Open();
                    string expression = @"INSERT INTO C_type
                                        (type)
                                        VALUES 
                                        (@type)";
                    SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                    cmd.Parameters.AddWithValue("type", type);
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "SELECT last_insert_rowid()";

                    CatalogTypeIndex = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            return CatalogTypeIndex;
        }

        static int GetCatalogID(int catalogPeriodID, int catalogTypeID)
        {
            int CatalogIndex;
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string expression = @"SELECT id
                                    FROM  Catalog
                                    WHERE period = @period AND type = @type";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                cmd.Parameters.AddWithValue("period", catalogPeriodID);
                cmd.Parameters.AddWithValue("type", catalogTypeID);
                SQLiteDataReader reader = cmd.ExecuteReader();

                if (!reader.HasRows) // если возвращается 0 - такого  нет в БД
                    CatalogIndex = 0;
                else
                {
                    reader.Read();
                    CatalogIndex = reader.GetInt32(0);
                    reader.Close();
                }
            }

            // если такого  нету в БД, создаём и получаем Id
            if (CatalogIndex == 0)
            {
                using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
                {
                    connection.Open();
                    string expression = @"INSERT INTO Catalog
                                        (period, type)
                                        VALUES 
                                        (@period, @type)";
                    SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                    cmd.Parameters.AddWithValue("period", catalogPeriodID);
                    cmd.Parameters.AddWithValue("type", catalogTypeID);
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "SELECT last_insert_rowid()";

                    CatalogIndex = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            return CatalogIndex;
        }

        static int GetCategoryID(string category)
        {
            int CategoryIndex;
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string expression = @"SELECT id
                                    FROM  P_category
                                    WHERE name = @category";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                cmd.Parameters.AddWithValue("category", category);
                SQLiteDataReader reader = cmd.ExecuteReader();

                if (!reader.HasRows) // если возвращается 0 - такого категории нет в БД
                    CategoryIndex = 0;
                else
                {
                    reader.Read();
                    CategoryIndex = reader.GetInt32(0);
                    reader.Close();
                }
            }

            // если такого категории нету в БД, создаём и получаем Id
            if (CategoryIndex == 0)
            {
                using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
                {
                    connection.Open();
                    string expression = @"INSERT INTO P_category
                                        (name)
                                        VALUES 
                                        (@category)";
                    SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                    cmd.Parameters.AddWithValue("category", category);
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "SELECT last_insert_rowid()";

                    CategoryIndex = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            return CategoryIndex;
        }

        static bool ExistProductInProductTable(string code)
        {
            bool existProduct;
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string expression = @"SELECT code
                                    FROM  Product
                                    WHERE code = @code";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                cmd.Parameters.AddWithValue("code", code);
                SQLiteDataReader reader = cmd.ExecuteReader();

                if (!reader.HasRows) // если возвращается 0 - такого продукта нет в БД
                    existProduct = false;
                else
                    existProduct = true;

                reader.Close();
            }
            return existProduct;
        }

        static void AddProductToProductTable(string code, string name, int categoryID)
        {
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string expression = @"INSERT INTO Product
                                        (code, name, category)
                                        VALUES 
                                        (@code, @name, @category)";
                SQLiteCommand cmd = new SQLiteCommand(expression, connection);
                cmd.Parameters.AddWithValue("code", code);
                cmd.Parameters.AddWithValue("name", name);
                cmd.Parameters.AddWithValue("category", categoryID);
                cmd.ExecuteNonQuery();
            }
        }

        static int ExistSuchProductInPriceTable(out int quantityBD, string code, double priceDC, double pricePC, int catalogID, bool discont)
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
                cmd.Parameters.AddWithValue("catalog", catalogID);
                cmd.Parameters.AddWithValue("discont", discont);
                SQLiteDataReader reader = cmd.ExecuteReader();

                if (!reader.HasRows) // если возвращается 0 - такого продукта нет в БД
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

        static void AddProductToPriceTable(string code, double priceDC, double pricePC, int catalogID, string quantity, bool discont, string description)
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
                cmd.Parameters.AddWithValue("catalog", catalogID);
                cmd.Parameters.AddWithValue("quantity", quantity);
                cmd.Parameters.AddWithValue("discont", discont ? 1 : 0);
                cmd.Parameters.AddWithValue("description", description);

                cmd.ExecuteNonQuery();
            }
        }

        static void UpdateProductInPriceTable(int productIDInPriceTable, int newQuantity)
        {
            using (SQLiteConnection connection = new SQLiteConnection(DataBase.ConStrDB))
            {
                connection.Open();
                string sql = @"UPDATE Price 
                               SET quantity = @newQuantity
                               WHERE id = @id";

                SQLiteCommand command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("id", productIDInPriceTable);
                command.Parameters.AddWithValue("newQuantity", newQuantity);
                command.ExecuteNonQuery();
            }
        }

        #endregion
    }

}