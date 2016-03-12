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

        // global variables to connect to database
        private static string uni_DSN = "uniPointDB";
        private static string jobBoss_DSN = "uniPointDB";
        private readonly static string username = "jbread";
        private readonly static string password = "Cloudy2Day";
        private readonly string uni_connectionString = "DSN=" + uni_DSN + ";UID=" + username + ";PWD=" + password;
        private readonly string jBoss_connectionString = "DSN=" + jobBoss_DSN + ";UID=" + username + ";PWD=" + password;
        // all of the fields that the cost will be calculated for
        private readonly string[] fields = { "Scrap Cost", "Quality Labor Cost", "Rework Cost", "MRB Cost", "Lost Revenue", "Wasted Capacity Cost", "Recovery Lot Cost", "Delivery Cost", "Reputation Cost" };        

        static List<ScrapItem> scrapList = new List<ScrapItem>();

        public CostOfQualityForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // initialize gridview
            for (int i = 0; i < fields.Length; i++)
            {
                DetailsDataGridView.Rows.Add();
                DetailsDataGridView.Rows[i].HeaderCell.Value = fields[i];
                DetailsDataGridView.Rows[i].Cells[0].Style.Format = "C";
                DetailsDataGridView.Rows[i].Cells[1].Style.Format = "#,##0 hrs";
            }
            DetailsDataGridView.Columns["Value"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // initialize dateTimePicker
            startDateTimePicker.Value = startDateTimePicker.Value.AddYears(-1);

            FillInData(new object(), new EventArgs());

            // add event handler for click
            calculateButton.Click += this.FillInData;
        }

        private void FillInData(object sender, EventArgs e)
        {
            string query;
            // calculate scrap cost
            /*
             * So far this only includes the internal operation costs
             * Does not include: Outside ops, reworks scraps (tracing back to the original op)
             */
            query = "SELECT SUM(unionTotal.totalCost), SUM(unionTotal.totalHours) FROM\n" +
                    "(\n" +
                    "SELECT Sum((regJobs.Act_Run_Labor+regJobs.Act_Setup_Labor+regJobs.Act_Machine_Burden+regJobs.Act_Labor_Burden)/(regJobs.Act_Run_Qty+regJobs.Act_Scrap_Qty)*scrapCountT.Act_Scrap_Qty) AS totalCost,\n" +
                    "\tSUM((regJobs.Act_Run_Labor_Hrs+regJobs.Act_Setup_Labor_Hrs)/(regJobs.Act_Run_Qty+regJobs.Act_Scrap_Qty)*scrapCountT.Act_Scrap_Qty) AS totalHours\n" +
                    "FROM [PRODUCTION].[dbo].[Job_Operation] AS scrapCountT\n" +
                    "LEFT OUTER JOIN PRODUCTION.dbo.Job_Operation as minRewOpT\n" +
                    "ON minRewOpT.Job = scrapCountT.Job AND minRewOpT.Sequence <= scrapCountT.Sequence\n" +
                    "INNER JOIN (\n" +
                    "	SELECT Job, MIN(minT.Sequence) AS minPoint\n" +
                    "	FROM PRODUCTION.dbo.Job_Operation as minT\n" +
                    "	WHERE CHARINDEX('R', minT.Operation_Service) > 0 AND CHARINDEX('R', minT.Job) > 0\n" +
                    "	GROUP BY Job\n" +
                    ") AS minTable\n" +
                    "ON scrapCountT.Job = minTable.Job\n" +
                    "LEFT OUTER JOIN PRODUCTION.dbo.Job_Operation AS regJobs\n" +
                    "ON regJobs.Job = SUBSTRING(scrapCountT.Job, 0, CHARINDEX('R', scrapCountT.Job)) AND regJobs.Run_Qty + regJobs.Act_Scrap_Qty > 0\n" +
                    "WHERE scrapCountT.Act_Scrap_Qty > 0 AND scrapCountT.Last_Updated >= '" + startDateTimePicker.Value.ToShortDateString() + "' AND scrapCountT.Last_Updated <= '" + endDateTimePicker.Value.ToShortDateString() + "'\n" +
                    "		AND ISNUMERIC((CASE WHEN(CHARINDEX('R', regJobs.Operation_Service) > 0) THEN SUBSTRING(regJobs.Operation_Service, 0, CHARINDEX('R', regJobs.Operation_Service)) ELSE regJobs.Operation_Service END)) = 1\n" +
                    "		AND scrapCountT.Last_Updated <= '12/1/2015' AND minRewOpT.Sequence = minTable.minPoint \n" +
                    "		AND CAST((CASE WHEN(CHARINDEX('R', regJobs.Operation_Service) > 0) THEN SUBSTRING(regJobs.Operation_Service, 0, CHARINDEX('R', regJobs.Operation_Service)) ELSE regJobs.Operation_Service END) AS int)\n" +
                    "			<= CAST(SUBSTRING(minRewOpT.Operation_Service, 0, CHARINDEX('R', minRewOpT.Operation_Service)) AS int)\n" +
                    "UNION ALL\n" +
                    "SELECT Sum((sumCountT.Act_Run_Labor+sumCountT.Act_Setup_Labor+sumCountT.Act_Machine_Burden+sumCountT.Act_Labor_Burden)/(sumCountT.Act_Run_Qty+sumCountT.Act_Scrap_Qty)*scrapCountT.Act_Scrap_Qty) AS totalCost,\n" +
                    "\tSum((sumCountT.Act_Run_Labor_Hrs+sumCountT.Act_Setup_Labor_Hrs)/(sumCountT.Act_Run_Qty+sumCountT.Act_Scrap_Qty)*scrapCountT.Act_Scrap_Qty) AS totalHours\n" +
                    "FROM [PRODUCTION].[dbo].[Job_Operation] AS scrapCountT\n" +
                    "LEFT OUTER JOIN [PRODUCTION].[dbo].[Job_Operation] AS sumCountT\n" +
                    "ON scrapCountT.[Job] = sumCountT.[Job] AND sumCountT.Sequence <= scrapCountT.Sequence AND sumCountT.Act_Run_Qty+sumCountT.Act_Scrap_Qty > 0\n" +
                    "WHERE scrapCountT.Act_Scrap_Qty > 0 AND scrapCountT.Last_Updated >= '" + startDateTimePicker.Value.ToShortDateString() + "' AND scrapCountT.Last_Updated <= '" + endDateTimePicker.Value.ToShortDateString() + "'\n" +
                    "UNION ALL\n" +
                    "SELECT ISNULL(SUM((mT.Act_Total_Cost/maxT.maxPoint)*jT.Act_Scrap_Qty), 0) AS totalCost,\n" +
                    "\tISNULL(0, 0) AS totalHours\n" +
                    "FROM PRODUCTION.dbo.Job_Operation jT\n" +
                    "LEFT OUTER JOIN PRODUCTION.dbo.Material_Req AS mT\n" +
                    "ON mT.Job = jT.Job\n" +
                    "LEFT OUTER JOIN (\n" +
                    "	SELECT Job, MAX(Act_Run_Qty) AS maxPoint\n" +
                    "	FROM PRODUCTION.dbo.Job_Operation\n" +
                    "	GROUP BY job\n" +
                    ") AS maxT\n" +
                    "ON maxT.Job = jT.Job\n" +
                    "WHERE jT.Act_Scrap_Qty > 0 AND jT.Last_Updated >= '" + startDateTimePicker.Value.ToShortDateString() + "' AND jT.Last_Updated <= '" + endDateTimePicker.Value.ToShortDateString() + "' AND maxT.maxPoint > 0\n" +
                    ") AS unionTotal;";
            ExecuteQuery(query, 0, DataType.DoubleT, jBoss_connectionString);
            /*
            // calculate labor cost
            query = "SELECT SUM(CASE WHEN (NCR_Date >= '" + startDateTimePicker.Value.ToString() + "' AND NCR_Date <='" + endDateTimePicker.Value.ToString() + "') THEN NC_processing_cost ELSE 0 END)+\n" +
                    "SUM(CASE WHEN (CPA_date >= '" + startDateTimePicker.Value.ToString() + "' AND CPA_date <='" + endDateTimePicker.Value.ToString() + "') THEN CPA_labor_cost ELSE 0 END)\n" +
                    "FROM uniPoint_Live.dbo.PT_NC\n" +
                    "FULL OUTER JOIN uniPoint_Live.dbo.PT_CPA\n" +
                    "ON 1=2;";
            ExecuteQuery(query, 1, DataType.DoubleT, uni_connectionString);

            // calculate rework cost
            query = "SELECT ISNULL(SUM(ISNULL(Act_Cost,0) + ISNULL(Addl_Cost_Act_Amt1,0) + ISNULL(Addl_Cost_Act_Amt2,0) + ISNULL(Job_Operation_Time.Act_Setup_Hrs*Setup_Labor_Rate,0) + ISNULL(Job_Operation_Time.Act_Run_Hrs*Run_Labor_Rate,0)), 0)\n" +
                    "FROM Production.dbo.Job\n" +
                    "INNER JOIN Production.dbo.Job_Operation\n" +
                    "ON Job.Job = Job_Operation.Job\n" +
                    "LEFT OUTER JOIN Production.dbo.PO_Detail\n" +
                    "INNER JOIN Production.dbo.Source\n" +
                    "ON PO_Detail.PO_Detail = Source.PO_Detail\n" +
                    "ON Job_Operation.Job_Operation = Source.Job_Operation\n" +
                    "LEFT OUTER JOIN Production.dbo.job_Operation_Time\n" +
                    "ON Job_Operation.Job_Operation = Job_Operation_Time.Job_Operation\n" +
                    "WHERE (Job_Operation.Operation_Service LIKE '%R%') AND (Job_Operation.Status = 'C') AND (NOT (Job.Part_Number LIKE 'NOT ISSUED')) AND\n" +
                    "(Job.Order_Date >= '" + startDateTimePicker.Value.ToString() + "' AND Job.Order_Date <= '" + endDateTimePicker.Value.ToString() + "' AND Job.Status <> 'Template');";
            ExecuteQuery(query, 2, DataType.DoubleT, jBoss_connectionString);

            // calculate MRB costs
            query = "SELECT SUM(feesT.Fee)\n" +
                    "FROM [uniPoint_Live].[dbo].[PT_NC] AS ncT\n" +
                    "LEFT OUTER JOIN PRODUCTION.dbo.Material as matT\n" +
                    "ON matT.Material = nct.Material\n" +
                    "LEFT OUTER JOIN ATIDelivery.dbo.Customer_Fees as feesT\n" +
                    "ON feesT.Customer = matT.Class\n" +
                    "WHERE Disposition = 'Customer MRB' AND ncT.NCR_Date >= '" + startDateTimePicker.Value.ToShortDateString() + "' AND ncT.NCR_Date <= '" + endDateTimePicker.Value.ToShortDateString() + "';";
            ExecuteQuery(query, 3, DataType.DoubleT, jBoss_connectionString);

            // calculate lost revenue cost
            query = "SELECT SUM(Act_Scrap_Qty*soT.Unit_Price) AS Total\n" +
                    "FROM [PRODUCTION].[dbo].[Job_Operation] AS scrapCountT\n" +
                    "LEFT OUTER JOIN	PRODUCTION.dbo.SO_Detail AS soT\n" +
                    "ON soT.Job = scrapCountT.Job\n" +
                    "WHERE scrapCountT.Act_Scrap_Qty > 0 AND scrapCountT.Last_Updated >= '" + startDateTimePicker.Value.ToShortDateString() + "' AND scrapCountT.Last_Updated <= '" + endDateTimePicker.Value.ToShortDateString() + "';";
            ExecuteQuery(query, 4, DataType.DoubleT, jBoss_connectionString);

            // calculate wasted capacity cost
            query = "SELECT Sum((sumCountT.Act_Labor_Burden)/(sumCountT.Act_Run_Qty + sumCountT.Act_Scrap_Qty)*scrapCountT.Act_Scrap_Qty)\n" +
                    "FROM [PRODUCTION].[dbo].[Job_Operation] AS scrapCountT\n" +
                    "LEFT OUTER JOIN [PRODUCTION].[dbo].[Job_Operation] AS sumCountT\n" +
                    "ON scrapCountT.[Job] = sumCountT.[Job] AND sumCountT.Sequence <= scrapCountT.Sequence\n" +
                    "WHERE scrapCountT.Act_Scrap_Qty > 0 AND scrapCountT.Last_Updated >= '" + startDateTimePicker.Value.ToString() + "' AND sumCountT.Act_Run_Qty + sumCountT.Act_Scrap_Qty > 0 AND scrapCountT.Last_Updated <= '" + endDateTimePicker.Value.ToString() + "';";
            ExecuteQuery(query, 5, DataType.DoubleT, jBoss_connectionString);

            // calculate recovery lot cost
            query = "SELECT 0";
            ExecuteQuery(query, 6, DataType.DoubleT, jBoss_connectionString);

            // calculate delivery cost
            query = "SELECT 0";
            ExecuteQuery(query, 7, DataType.DoubleT, jBoss_connectionString);

            // calcualte reputation cost
            query = "SELECT 0";
            ExecuteQuery(query, 8, DataType.DoubleT, jBoss_connectionString);*/
        }

        private void ExecuteQuery(string query, int rowIndex, DataType type, string connectionString)
        {
            using (OdbcConnection conn = new OdbcConnection(connectionString))
            {
                conn.Open();

                // run query
                OdbcCommand comm = new OdbcCommand(query, conn);
                OdbcDataReader reader = comm.ExecuteReader();

                // cell references
                DataGridViewCell currentCellValue = DetailsDataGridView.Rows[rowIndex].Cells[0];
                DataGridViewCell currentCellHours = DetailsDataGridView.Rows[rowIndex].Cells[1];

                // read row (check it exists)
                if(reader.Read() == null)
                    return;

                // current column index
                int col = 0;

                switch (Type.GetTypeCode(reader.GetFieldType(col)))
                {
                    case TypeCode.Decimal:
                        currentCellValue.Value = reader.GetDecimal(col);
                        break;
                    case TypeCode.Double:
                        currentCellValue.Value = reader.GetDouble(col);
                        break;
                    case TypeCode.DateTime:
                        currentCellValue.Value = reader.GetDateTime(col);
                        break;
                    case TypeCode.String:
                        currentCellValue.Value = reader.GetString(col);
                        break;
                    case TypeCode.Int32:
                        currentCellValue.Value = reader.GetInt32(col);
                        break;
                }

                // check if row has a second value (index 1)
                if (reader.IsDBNull(1))
                    return;

                // increase column reference
                col++;

                switch (Type.GetTypeCode(reader.GetFieldType(col)))
                {
                    case TypeCode.Decimal:
                        currentCellHours.Value = reader.GetDecimal(col);
                        break;
                    case TypeCode.Double:
                        currentCellHours.Value = reader.GetDouble(col);
                        break;
                    case TypeCode.DateTime:
                        currentCellHours.Value = reader.GetDateTime(col);
                        break;
                    case TypeCode.String:
                        currentCellHours.Value = reader.GetString(col);
                        break;
                    case TypeCode.Int32:
                        currentCellHours.Value = reader.GetInt32(col);
                        break;
                }

            }
        }
    }
}
