using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace trans_match
{
    class Program
    {
        static DataTable buy = new DataTable();
        static DataTable sell = new DataTable();
        static DataTable cg = new DataTable();
        static void Main(string[] args)
        {
            //string buy_csv = @"D:\myCode\misc\CG\CG\CG\buy.csv";
            string buy_csv = @"..\..\..\buy.csv";
            string sell_csv = @"..\..\..\sell.csv";
            string cg_csv = @"..\..\..\cg.csv";
            string buy_out_csv = @"..\..\..\buy_out.csv";
            buy = LoadData(buy_csv);
            sell = LoadData(sell_csv);
            cg = reconcileData(buy, sell);
            WriteData(cg, cg_csv);
            WriteData(buy, buy_out_csv);
        }

        private static void WriteData(DataTable dt, String file)
        {
            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dt.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                sb.AppendLine(string.Join(",", fields));
            }

            File.WriteAllText(file, sb.ToString());
        }

        private static DataTable reconcileData(DataTable buy, DataTable sell)
        {
            DataTable merged = new DataTable();
            string[] colFields = { "Name", "Qty", "BuyDate", "BuyRate", "BuyAmt", "BuyCharges", "SellDate", "SellRate", "SellAmt", "SellCharges" };
            string[] intFields = { "Qty" };
            string[] floatFields = { "BuyRate", "BuyAmt", "BuyCharges", "SellRate", "SellAmt", "SellCharges" };
            foreach (string column in colFields)
            {
                DataColumn datecolumn = new DataColumn(column);
                datecolumn.AllowDBNull = true;
                if (intFields.Contains(column))
                {
                    datecolumn.DataType = Type.GetType("System.Int32");
                }
                else if (floatFields.Contains(column))
                {
                    datecolumn.DataType = Type.GetType("System.Decimal");
                }
                merged.Columns.Add(datecolumn);
            }

            foreach (DataRow sItem in sell.Rows)
            {
                String sname = sItem.Field<string>("Name");
                foreach (DataRow bItem in buy.AsEnumerable()
                                            .Where(row => row.Field<string>("Name") == sname &&
                                                          row.Field<int>("Qty") > 0))
                {
                    // find matching name
                    // String sname = sItem.Field<string>("Name");
                    String bname = bItem.Field<string>("Name");
                    int sQty = sItem.Field<int>("Qty");
                    int bQty = bItem.Field<int>("Qty");
                    decimal bAmt = bItem.Field<decimal>("Amt");
                    decimal bCharges = bItem.Field<decimal>("Charges");
                    decimal sAmt = sItem.Field<decimal>("Amt");
                    decimal sCharges = sItem.Field<decimal>("Charges");

                    if (sQty == bQty)
                    {
                        DataRow dr = merged.NewRow();
                        dr["Name"] = sname;
                        dr["Qty"] = sQty;
                        dr["BuyDate"] = bItem["Date"];
                        dr["BuyRate"] = bItem["Rate"];
                        dr["BuyAmt"] = bItem["Amt"];
                        dr["BuyCharges"] = bItem["Charges"];
                        dr["SellDate"] = sItem["Date"];
                        dr["SellRate"] = sItem["Rate"];
                        dr["SellAmt"] = sItem["Amt"];
                        dr["SellCharges"] = sItem["Charges"];                        

                        bItem["Qty"] = 0;
                        merged.Rows.Add(dr);
                        break;
                    }
                    else if (sQty < bQty)
                    {

                        DataRow dr = merged.NewRow();
                        dr["Name"] = sname;
                        dr["Qty"] = sQty;
                        dr["BuyDate"] = bItem["Date"];
                        dr["BuyRate"] = bItem["Rate"];
                        dr["BuyAmt"] = bAmt * sQty / bQty;
                        dr["BuyCharges"] = bCharges * sQty / bQty;
                        dr["SellDate"] = sItem["Date"];
                        dr["SellRate"] = sItem["Rate"];
                        dr["SellAmt"] = sItem["Amt"];
                        dr["SellCharges"] = sItem["Charges"];                        

                        bItem["Qty"] = bQty - sQty;
                        bItem["Amt"] = bAmt - dr.Field<decimal>("BuyAmt");
                        bItem["Charges"] = bCharges - dr.Field<decimal>("BuyCharges");

                        merged.Rows.Add(dr);
                        break;
                    }
                    else
                    {
                        SellMore(merged, sItem, buy.AsEnumerable()
                                            .Where(row => row.Field<string>("Name") == sname &&
                                                          row.Field<int>("Qty") > 0));
                        break;
                    }
                }
            }

            return merged;
        }

        private static void SellMore(DataTable merged, DataRow sItem, EnumerableRowCollection<DataRow> buys)
        {
            String sname = sItem.Field<string>("Name");
            int sQty = sItem.Field<int>("Qty");            
            decimal sAmt = sItem.Field<decimal>("Amt");
            decimal sCharges = sItem.Field<decimal>("Charges");
            int sQtyLeft = sQty;

            foreach (DataRow bItem in buys)
            {
                int bQty = bItem.Field<int>("Qty");
                decimal bAmt = bItem.Field<decimal>("Amt");
                decimal bCharges = bItem.Field<decimal>("Charges");

                if(sQtyLeft > bQty)
                {
                    // transaction is fully sold
                    sQtyLeft = sQtyLeft - bQty;
                    bItem["Qty"] = 0;
                   
                    DataRow dr = merged.NewRow();
                    dr["Name"] = sname;
                    dr["Qty"] = bQty;
                    dr["BuyDate"] = bItem["Date"];
                    dr["BuyRate"] = bItem["Rate"];
                    dr["BuyAmt"] = bItem["Amt"];
                    dr["BuyCharges"] = bItem["Charges"];
                    dr["SellDate"] = sItem["Date"];
                    dr["SellRate"] = sItem["Rate"];
                    dr["SellAmt"] = sAmt * bQty / sQty;
                    dr["SellCharges"] = sCharges * bQty / sQty;                               
                    merged.Rows.Add(dr);                    

                }
                else if(sQtyLeft == bQty)
                {
                    bItem["Qty"] = 0;

                    DataRow dr = merged.NewRow();
                    dr["Name"] = sname;
                    dr["Qty"] = bQty;
                    dr["BuyDate"] = bItem["Date"];
                    dr["BuyRate"] = bItem["Rate"];
                    dr["BuyAmt"] = bItem["Amt"];
                    dr["BuyCharges"] = bItem["Charges"];
                    dr["SellDate"] = sItem["Date"];
                    dr["SellRate"] = sItem["Rate"];
                    dr["SellAmt"] = sAmt * bQty / sQty;
                    dr["SellCharges"] = sCharges * bQty / sQty;                    
                    sQtyLeft = 0;
                    merged.Rows.Add(dr);
                    break;
                }
                else
                {
                    // partial sold for the transaction                    
                    bItem["Qty"] = bQty - sQtyLeft;
                    bItem["Amt"] = bAmt - (bAmt * sQtyLeft / bQty);
                    bItem["Charges"] = bCharges - (bCharges * sQtyLeft / bQty);

                    DataRow dr = merged.NewRow();
                    dr["Name"] = sname;
                    dr["Qty"] = sQtyLeft;
                    dr["BuyDate"] = bItem["Date"];
                    dr["BuyRate"] = bItem["Rate"];
                    dr["BuyAmt"] = sQtyLeft * bItem.Field<decimal>("Rate");
                    dr["BuyCharges"] = bCharges * sQtyLeft/bQty;
                    dr["SellDate"] = sItem["Date"];
                    dr["SellRate"] = sItem["Rate"];
                    dr["SellAmt"] = sItem.Field<decimal>("Amt") * sQtyLeft/sQty;
                    dr["SellCharges"] = sItem.Field<decimal>("Charges") * sQtyLeft / sQty;                    
                    sQtyLeft = 0;
                    merged.Rows.Add(dr);
                    break;
                }
            }
        }

        private static DataTable LoadData(String file)
        {
            DataTable csvData = new DataTable();
            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(file))
                {
                    csvReader.SetDelimiters(new string[] { "," });
                    csvReader.HasFieldsEnclosedInQuotes = true;
                    string[] colFields1 = csvReader.ReadFields();
                    string[] colFields = { "Name", "Date", "Qty", "Rate", "Amt", "Charges" };
                    string[] intFields = { "Qty" };
                    string[] floatFields = { "Rate", "Amt", "Charges" };
                    foreach (string column in colFields)
                    {
                        DataColumn datecolumn = new DataColumn(column);
                        datecolumn.AllowDBNull = true;
                        if (intFields.Contains(column))
                        {
                            datecolumn.DataType = Type.GetType("System.Int32");
                        }
                        else if (floatFields.Contains(column))
                        {
                            datecolumn.DataType = Type.GetType("System.Decimal");
                        }
                        csvData.Columns.Add(datecolumn);
                    }

                    while (!csvReader.EndOfData)
                    {
                        string[] fieldData = csvReader.ReadFields();
                        //Making empty value as null
                        for (int i = 0; i < fieldData.Length; i++)
                        {
                            if (fieldData[i] == "")
                            {
                                fieldData[i] = null;
                            }
                        }
                        csvData.Rows.Add(fieldData);
                    }
                }
                return csvData;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                throw;
            }
        }
    }
}
