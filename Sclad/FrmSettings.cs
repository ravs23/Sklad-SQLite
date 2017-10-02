using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;

namespace Sklad
{
    public partial class FrmSettings : Form
    {
        string defaultFolderPrices = @"Prices\";
        public FrmSettings()
        {
            InitializeComponent();
        }

        private void FrmSettings_Load(object sender, EventArgs e)
        {
            tbDiscont.Text = Settings.Discount.ToString();
            chbAddZero.Checked = Settings.DisplayCatalogPeriodsWithZero;
            tbFolderPrices.Text = Settings.FolderPrices;
            chbLog.Checked = Settings.Logging;

            folderBrowserDialog_Prices.Description = "Выберите папку с прайс-листами:";

        }

        private void btnFolders_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog_Prices.ShowDialog(this) == DialogResult.OK)
            {
                if (folderBrowserDialog_Prices.SelectedPath.ToLower() != Environment.CurrentDirectory.ToLower())
                {
                    folderBrowserDialog_Prices.SelectedPath = folderBrowserDialog_Prices.SelectedPath.TrimEnd('\\');

                    if (folderBrowserDialog_Prices.SelectedPath.ToLower().Contains(Environment.CurrentDirectory.ToLower()))
                    {
                        string folder = folderBrowserDialog_Prices.SelectedPath.ToLower().Replace(Environment.CurrentDirectory.ToLower(), "");
                        folder = folder.TrimStart('\\') + "\\";
                        tbFolderPrices.Text = folder.Substring(0, 1).ToUpper() + folder.Substring(1, folder.Length - 1);
                    }
                    else
                        tbFolderPrices.Text = folderBrowserDialog_Prices.SelectedPath + @"\";
                }
                else
                    tbFolderPrices.Text = defaultFolderPrices;
            }

        }

        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            if (CheckInputData())
            {
                this.DialogResult = DialogResult.OK;
            }
        }

        bool CheckInputData()
        {
            if (!Int32.TryParse(tbDiscont.Text, out int discount) || !(discount >= 0 && discount < 100))
            {
                MessageBox.Show("Скидка в пределах [0 ... 99] %");
                return false;
            }

            // Проверяем путь
            try
            {
                if (!Directory.Exists(tbFolderPrices.Text))
                {
                    Directory.CreateDirectory(tbFolderPrices.Text);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            if (!Directory.Exists(tbFolderPrices.Text))
                return false;


            return true;
        }

        private void tbFolderPrices_Leave(object sender, EventArgs e)
        {
            if (tbFolderPrices.Text == "\\" || tbFolderPrices.Text.Trim() == string.Empty)
                tbFolderPrices.Text = defaultFolderPrices;
            tbFolderPrices.Text = tbFolderPrices.Text.TrimEnd('\\');
            tbFolderPrices.Text += "\\";
        }

        private async void btnOptimiztion_Click(object sender, EventArgs e)
        {
            btnOptimiztion.Enabled = false;
            string holdTxt = this.Text;
            this.Text += " [ Оптимизация ... ]";

            await SkladBase.OptimizationAsync();

            await Category.MakeListAsync();
            await CatalogPeriod.MakeListAsync();
            await Catalog.MakeListAsync();

            MessageBox.Show("Оптимизация выполнена.", "Оптимизация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Text = holdTxt;
            btnOptimiztion.Enabled = true;
        }
    }
}
