using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CostOfQuality
{
    class ScrapItem
    {
        public enum ScrapType {NC, CPA};

        ScrapType type;
        string serialNum;
        DateTime scrapDate;
        string partNum;
        string job;
        string op;
        double qtyScrapped;
        string origin;
        string originRef;
        string cause;

        public ScrapItem(ScrapType type, string serialNum, DateTime scrapDate, string partNum, string job, string op, double qtyScrapped, string origin, string originRef, string cause)
        {
            this.type = type;
            this.serialNum = serialNum;
            this.scrapDate = scrapDate;
            this.partNum = partNum;
            this.job = job;
            this.op = op;
            this.qtyScrapped = qtyScrapped;
            this.origin = origin;
            this.originRef = originRef;
            this.cause = cause;
        }
    }
}