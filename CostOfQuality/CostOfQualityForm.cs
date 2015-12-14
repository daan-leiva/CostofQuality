using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.Odbc;
namespace CostOfQuality
{
    public partial class CostOfQualityForm : Form
    {
        enum DataType { DoubleT, StringT, DateT };

        private static string uni_DSN = "uniPointDB";
        private static string jobBoss_DSN = "uniPointDB";
        private readonly static string username = "jbread";
        private readonly static string password = "Cloudy2Day";
        private readonly string[] fields = { "Scrap", "Labor", "Rework", "Recovery", "MRB Costs", "Lost Revenue", "Wasted Capacity", "Recovery Lot", "Delivery", "Reputation" };
        private readonly string uni_connectionString = "DSN=" + uni_DSN + ";UID=" + username + ";PWD=" + password;
        private readonly string jBoss_connectionString = "DSN=" + jobBoss_DSN + ";UID=" + username + ";PWD=" + password;

        static List<ScrapItem> scrapList = new List<ScrapItem>();

        public CostOfQualityForm()
        {
            InitializeComponent();
        }

        private void Calculate_Click(object sender, EventArgs e)
        {
            FillInData();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // initialize gridview
            DetailsDataGridView.Columns.Add("Value", "Value");

            for (int i = 0; i < fields.Length; i++)
            {
                DetailsDataGridView.Rows.Add();
                DetailsDataGridView.Rows[i].HeaderCell.Value = fields[i];
            }

            // initialize dateTimePicker
            startDateTimePicker.Value = startDateTimePicker.Value.AddYears(-1);

            Calculate_Click(new Object(), new EventArgs());
        }

        private void FillInData()
        {
            string query;
            // calculate scrap cost
            query = "SELECT 100*" + startDateTimePicker.Value.DayOfYear*endDateTimePicker.Value.DayOfYear;
            ExecuteQuery(query, 0, DataType.DoubleT);

            // calculate labor cost
            query = "SELECT 100*" + startDateTimePicker.Value.DayOfYear * endDateTimePicker.Value.DayOfYear;
            ExecuteQuery(query, 1, DataType.DoubleT);

            // calculate rework cost
            query = "SELECT 100*" + startDateTimePicker.Value.DayOfYear * endDateTimePicker.Value.DayOfYear;
            ExecuteQuery(query, 2, DataType.DoubleT);

            // calculate recovery cost
            query = "SELECT 1000*" + startDateTimePicker.Value.DayOfYear * endDateTimePicker.Value.DayOfYear;
            ExecuteQuery(query, 3, DataType.DoubleT);

            // calculate MRB costs
            query = "SELECT 100*" + startDateTimePicker.Value.DayOfYear * endDateTimePicker.Value.DayOfYear;
            ExecuteQuery(query, 4, DataType.DoubleT);

            // calculate lost revenue cost
            query = "SELECT 100*" + startDateTimePicker.Value.DayOfYear * endDateTimePicker.Value.DayOfYear;
            ExecuteQuery(query, 5, DataType.DoubleT);

            // calculate wasted capacity cost
            query = "SELECT 100*" + startDateTimePicker.Value.DayOfYear * endDateTimePicker.Value.DayOfYear;
            ExecuteQuery(query, 6, DataType.DoubleT);

            // calculate recovery lot cost
            query = "SELECT 100*" + startDateTimePicker.Value.DayOfYear * endDateTimePicker.Value.DayOfYear;
            ExecuteQuery(query, 7, DataType.DoubleT);

            // calculate delivery cost
            query = "SELECT 100*" + startDateTimePicker.Value.DayOfYear * endDateTimePicker.Value.DayOfYear;
            ExecuteQuery(query, 8, DataType.DoubleT);

            // calcualte reputation cost
            query = "SELECT 100*" + startDateTimePicker.Value.DayOfYear * endDateTimePicker.Value.DayOfYear;
            ExecuteQuery(query, 9, DataType.DoubleT);
        }

        private void ExecuteQuery(string query, int rowIndex, DataType type)
        {
            using(OdbcConnection conn = new OdbcConnection(jBoss_connectionString))
            {
                conn.Open();

                OdbcCommand comm = new OdbcCommand(query, conn);

                OdbcDataReader reader = comm.ExecuteReader();

                DataGridViewCell currentCell = DetailsDataGridView.Rows[rowIndex].Cells[0];

                // read value
                reader.Read();

                switch(type)
                {
                    case DataType.DateT:
                        currentCell.Value = reader.GetDateTime(0);
                        break;
                    case DataType.DoubleT:
                        currentCell.Value = reader.GetDouble(0);
                        break;
                    case DataType.StringT:
                        currentCell.Value = reader.GetString(0);
                        break;
                }
            }
        }

        private void PopulateNCCPAData()
        {
            // first find the ncs from this year
            using (OdbcConnection conn = new OdbcConnection(uni_connectionString))
            {
                // open connection to database
                conn.Open();

                // query for nc data
                string query = "SELECT NCR,NCR_Date, Material, Job, Reference, Qty_Scrap, Origin, Origin_ref, Origin_cause\n" +
                                "FROM uniPoint_Live.dbo.PT_NC\n" +
                                "WHERE NCR_Date >= " + startDateTimePicker.Value.ToShortDateString() + " AND NCR_Date <= " + endDateTimePicker.Value.ToShortDateString() + ";";

                OdbcCommand com = new OdbcCommand(query, conn);
                OdbcDataReader reader = com.ExecuteReader();

                while (reader.Read())
                {

                }
            }
        }
    }
}
