using System;
using System.Windows.Forms;

namespace HourlyStatistics
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        int requestCount(string data, string IP)
        {
            int count = 0;

            var hour = dateTime.getHours();
            hour = ("0" + hour).slice(-2);
            var endDate = dateTime.toDateString() + "T" + hour + "%3A00%3A00%2B03%3A00";

            dateTime.setHours(dateTime.getHours() - 1);
            hour = dateTime.getHours();
            hour = ("0" + hour).slice(-2);
            var strDate = dateTime.toDateString() + "T" + hour + "%3A00%3A00%2B03%3A00";

            string url = $@"http://{IP}/archive/tracks/count?dateStart={strDate}&dateEnd={endDate}";


            return count;
        }

        private void request_Click(object sender, EventArgs e)
        {
            ip.Enabled = false; 
            date.Enabled = false;





        }

        private void clear_Click(object sender, EventArgs e)
        {

        }

        private void save_Click(object sender, EventArgs e)
        {

        }
    }
}
