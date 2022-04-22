using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;


//my
using System.Xml;
using System.Globalization;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.IO;


namespace first
{
    //Таблицы товаров на складе (поступление и отгрузка)
    public class Operat
    {
        public int Type { get; private set; } //Тип операции
        public List<List<Product>> Stores { get; private set; } //Склады
        public Operat(int TypeOperation, List<List<Product>> stores)
        {
            this.Type = TypeOperation;
            this.Stores = stores;
        }
        public List<Product> ToMyList() //Для вывода в таблицу
        {
            List < Product > temp = new List<Product> ();

            foreach (List<Product> item in this.Stores)
                temp.AddRange(item);
            return temp;
        }

    }


    // Товар
    public class Product : IComparable
    {
        public string Store { get; private set; } //Наименование склада
        public string Name { get; private set; } //Наименование товара
        public int Count { get; private set; } //Количество
        public float Weight { get; private set; } //Масса
        public string Fragile { get; private set; } //Хрупкость
        public DateTime Date { get; private set; } //Дата

        public int CompareTo(object? obj) ///Сортировка по дате
        {
            return this.Date.CompareTo(((Product)obj).Date);
        }


        public Product(String store, XmlNode parent)
        {

            this.Store = store; 

            foreach (XmlNode child in parent.ChildNodes)
            {
                switch (child.Name)
                {
                    case "product_name":
                        this.Name = child.InnerText;
                        break;
                    case "count":
                        this.Count = int.Parse(child.InnerText);
                        break;
                    case "m":
                        this.Weight = float.Parse(child.InnerText, CultureInfo.InvariantCulture);
                        break;
                    case "fragile":
                        this.Fragile = child.InnerText;
                        break;
                    case "date":
                        this.Date = DateTime.Parse(child.InnerText);
                        break;
                }
            }
        }
    }


    //таблица "Запасы товаров на складах"
    public class Answer
    {
        public string Name { get; private set; } //наименование
        public int Count { get; private set; } //количество
        public float Weight { get; private set; } //масса

        public Answer(string name, int count, float weight)
        {
            this.Name = name;
            this.Count = count;
            this.Weight = weight;
        }

        //Для подсчета общего количества и массы
        public int AddCount(Product product, int operation)
        {
            this.Count = this.Count + operation * product.Count;
            this.Weight = this.Weight + operation * product.Count * product.Weight;

            return 0;
        }
    }



    public partial class MainWindow : Window
    {
        ObservableCollection<Answer> answer;  //Таблица "Запасы товаров на складах"
        Operat receipt; // Таблица "Поступившие товары на склад"
        Operat shipment; // Таблица "Отгруженные товары со склада" 

        public MainWindow()
        {
            InitializeComponent();
           
            receipt = LoadXml("OOOSealReceipt.xml", true); //Чтение xml файла приемки товара
            shipment = LoadXml("OOOSealShipment.xml", false); //Чтение xml файла отгрузки товара
            WPFDataGrid1.ItemsSource = receipt.ToMyList(); //Отображение таблицы "Поступившие товары на склад"
            WPFDataGrid2.ItemsSource = shipment.ToMyList(); //Отображение таблицы "Отгруженные товары со склада"
        }


        private Operat LoadXml(string filename, bool TypeOperation) //Чтение xml файла, filename - имя файла, TypeOperation - тип операции: true - прием товара, false - отгрузка товара
        {
            List<List<Product>> stores;  //Вспомогательный список складов
            List<Product> products; //Вспомогательный список товаров

            string nameStore = "";

            var document = new XmlDocument();

            stores = new List<List<Product>>();  //создаю список списков товаров (Список складов)  
            document.Load(filename); //открываю текущий файл
           
            foreach (XmlNode subtag in document.DocumentElement.ChildNodes)  //перебираем подэлементы <return>
            {
                products = new List<Product>(); //создаю список товаров (Склад)
                foreach (XmlNode subsubtag in subtag.ChildNodes) //перебираем подэлементы <storage>
                {
                    if (subsubtag.Name == "storage_name") //Записываем наименование склада
                        nameStore = subsubtag.InnerText;
           
           
                    if (subsubtag.Name == "product") //Если нашли описание товара, то парсим его
                        products.Add(new Product(nameStore, subsubtag)); //добавляем новый товар в список (в склад)
                }
                products.Sort(); //Сортирую список товаров (склад) по дате
                stores.Add(products); //Добавляю список товаров (склад) в список складов
            }
           
            if (TypeOperation)
                 return new Operat(1, stores); //прием товара
            else
                return new Operat(-1, stores); //отгрузка товара
        }




        private void RefreshDataGrid3Button_Click(object sender, RoutedEventArgs e)
        {

            //LoadEmployees();
            answer = new ObservableCollection<Answer>();

            //Получаем значение из DatePicker
            DateTime? date1 = DatePicker1.SelectedDate;


            if (date1 == null)
            {
                return;
            }

            //Итоговое количество и масса товаров
            int itogC = 0;
            float itogW = 0;

            int findIndex; //Переменная для поиска элементов 

            
            foreach (Operat op in new Operat[] { receipt , shipment })  //Перебор по таблицам (сначала приемка, потом отгрузка)
            {

                foreach (List<Product> item in op.Stores) //Перебор по складам
                {
                    foreach (Product item2 in item) //Перебор по товарам в складе
                    {
                        if (item2.Date > date1) //Считаем до тех пор, пока текущая дата меньше заданной
                            break;

                        
                        findIndex = answer.IndexOf(answer.Where(x => x.Name == item2.Name).FirstOrDefault()); //Ищу позицию в итоговой таблице текущего товара
                        //Если данного товара ещё нет в таблице, то создаем и добавляем его
                        if (findIndex < 0)
                            answer.Add(new Answer(item2.Name, item2.Count, item2.Weight * item2.Count));
                        else
                            answer[findIndex].AddCount(item2, op.Type); //Если есть, то проводим соответствующую операцию с очередным товаром

                        //Подсчитываем итоговое количество и массу товаров
                        itogC = itogC + op.Type * item2.Count;
                        itogW = itogW + op.Type * item2.Count * item2.Weight;
                    }
                }
            }


            //Добавляем в таблицу итоговое количество и массу товаров
            answer.Add(new Answer("Итог", itogC, itogW));



            //Отображаем таблицу "Запасы товаров на складах"
            WPFDataGrid3.ItemsSource = answer;
            SaveButton.IsEnabled = true; //Разблокирование кнопки сохранения в файле excel
        }


        //Сохранение в Excel файл
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Office.Interop.Excel.Application excelapp = new Microsoft.Office.Interop.Excel.Application(); //Инициализация excel
            excelapp.SheetsInNewWorkbook = 3; // Устанавливаем свойство, что в книге будет 3 листа

            Microsoft.Office.Interop.Excel.Workbook excelappworkbook = excelapp.Workbooks.Add(); //создаем новую книгу excel
            Microsoft.Office.Interop.Excel.Sheets excelsheets = excelappworkbook.Worksheets; // получаем все листы книги excel
            Microsoft.Office.Interop.Excel.Worksheet excelworksheet;  // Текущий рабочий лист Excel
                        


            excelworksheet = excelsheets.get_Item(1); //Выбираем Лист 1

            //Выводим заголовок таблицы "Запасы товаров на складах"
            excelworksheet.Rows[1].Columns[1] = "Название склада";
            excelworksheet.Rows[1].Columns[2] = "Наименование товара";
            excelworksheet.Rows[1].Columns[3] = "Количество товара, шт";
            excelworksheet.Rows[1].Columns[4] = "Масса 1шт, кг";
            excelworksheet.Rows[1].Columns[5] = "Хрупкое, да/нет";
            excelworksheet.Rows[1].Columns[6] = "Дата поступления на склад";

            //Выводим таблицу "Запасы товаров на складах"
            int index = 0;
            foreach (List<Product> items in receipt.Stores)
            {
                foreach (Product item in items)
                {
                    excelworksheet.Rows[index + 2].Columns[1] = item.Store;
                    excelworksheet.Rows[index + 2].Columns[2] = item.Name;
                    excelworksheet.Rows[index + 2].Columns[3] = item.Count;
                    excelworksheet.Rows[index + 2].Columns[4] = item.Weight;
                    excelworksheet.Rows[index + 2].Columns[5] = item.Fragile;
                    excelworksheet.Rows[index + 2].Columns[6] = item.Date;
                    index++;
                }

            }
            excelworksheet.Columns.EntireColumn.AutoFit();
            excelworksheet.Name = "Запасы товаров на складах";




            excelworksheet = excelsheets.get_Item(2); //Выбираем Лист 2

            //Выводим заголовок таблицы "Поступившие товары на склад"
            excelworksheet.Rows[1].Columns[1] = "Название склада";
            excelworksheet.Rows[1].Columns[2] = "Наименование товара";
            excelworksheet.Rows[1].Columns[3] = "Количество товара, шт";
            excelworksheet.Rows[1].Columns[4] = "Масса 1шт, кг";
            excelworksheet.Rows[1].Columns[5] = "Хрупкое, да/нет";
            excelworksheet.Rows[1].Columns[6] = "Дата отгрузки со склада";

            index = 0; 
            // Выводим таблицу "Поступившие товары на склад"
            foreach (List<Product> items in shipment.Stores)
            {
                foreach (Product item in items)
                {
                    excelworksheet.Rows[index + 2].Columns[1] = item.Store;
                    excelworksheet.Rows[index + 2].Columns[2] = item.Name;
                    excelworksheet.Rows[index + 2].Columns[3] = item.Count;
                    excelworksheet.Rows[index + 2].Columns[4] = item.Weight;
                    excelworksheet.Rows[index + 2].Columns[5] = item.Fragile;
                    excelworksheet.Rows[index + 2].Columns[6] = item.Date;
                    index++;
                }

            }
            excelworksheet.Columns.EntireColumn.AutoFit();
            excelworksheet.Name = "Поступившие товары на склад";


            excelworksheet = excelsheets.get_Item(3); //Выбираем лист 3

            //Выводим заголовок таблицы "Отгруженные товары со склада" 
            excelworksheet.Rows[1].Columns[1] = "";
            excelworksheet.Rows[1].Columns[2] = "Количество товара, шт";
            excelworksheet.Rows[1].Columns[3] = "Масса всего, кг";

             // Выводим таблицу "Отгруженные товары со склада" 
            for (int i = 0; i < answer.Count(); i++)
            {
                excelworksheet.Rows[i + 2].Columns[1] = answer[i].Name;
                excelworksheet.Rows[i + 2].Columns[2] = answer[i].Count;
                excelworksheet.Rows[i + 2].Columns[3] = answer[i].Weight;
            }
            excelworksheet.Columns.EntireColumn.AutoFit();
            excelworksheet.Name = "Отгруженные товары со склада";


            //готовимся к сохранению файла
            excelapp.AlertBeforeOverwriting = false;
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel file (*.xlsx)|*.xlsx";
            saveFileDialog.InitialDirectory = Directory.GetCurrentDirectory();


            excelapp.DisplayAlerts = false; //отключить проверку сохранения документа поверх существующего, это сделает saveFileDialog
            if (saveFileDialog.ShowDialog() == true)
                excelappworkbook.SaveAs(saveFileDialog.FileName, Microsoft.Office.Interop.Excel.XlFileFormat.xlOpenXMLWorkbook);
            excelapp.DisplayAlerts = true; //включить проверку сохранения документа поверх существующего




            //Закрываем Excel и очищаем весь мусор
            excelapp.Quit();
            excelapp = null;
            excelappworkbook = null;
            excelsheets = null;
            excelworksheet = null;

            MessageBox.Show("Процесс сохранения завершен!");
        }
    }
}
