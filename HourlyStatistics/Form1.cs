using System;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;


namespace HourlyStatistics
{
    public partial class Ui : Form
    {
        CommonOpenFileDialog dialog = new CommonOpenFileDialog();

        public Ui()
        {
            InitializeComponent();
            date.Text = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            dataGridView1.Rows.Clear();
        }

        private void request_Click(object sender, EventArgs e)
        {
            ip.Enabled = false;
            date.Enabled = false;
            request.Enabled = false;
            clear.Enabled = false;
            save.Enabled = false;

            progressBar1.Value = 0;

            DateTime scheduleDate;
            if (DateTime.TryParseExact(date.Text, "yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out scheduleDate))
            {
                DateTime myDate = DateTime.ParseExact(date.Text + " 00:00:00", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                if (myDate < DateTime.Now) 
                {
                    Match match = Regex.Match(ip.Text, @"\b((([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(\.)){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5]))\b");
                    if (match.Success)
                    {
                        PingReply pr = new Ping().Send(ip.Text, 5000);
                        if (pr.Status == IPStatus.Success)
                        {
                            int rowNumbe = dataGridView1.Rows.Add();
                            dataGridView1.FirstDisplayedScrollingRowIndex = rowNumbe;
                            try
                            {
                                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create($"http://{ip.Text}/unitinfo/api/unitinfo");
                                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                                using (StreamReader stream = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                                {
                                    string factorJson = stream.ReadToEnd();
                                    var datajson = new JavaScriptSerializer().Deserialize<dynamic>(factorJson);
                                    string factoryNumber = datajson["unit"]["factoryNumber"];
                                    string serialNumber = datajson["certificate"]["serialNumber"];
                                    dataGridView1.Rows[rowNumbe].Cells[0].Value = serialNumber + " " + factoryNumber + " " + ip.Text;
                                }
                            }
                            catch
                            {
                                MessageBox.Show("Not a factor.", "IP unavailable.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            string factorZ = "%2B03%3A00";
                            try
                            {
                                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create($"http://{ip.Text}/systemmanager/api/Time/timezones/current");
                                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                                using (StreamReader stream = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                                {
                                    string factorJson = stream.ReadToEnd();
                                    var datajson = new JavaScriptSerializer().Deserialize<dynamic>(factorJson);
                                    string factorZone = datajson["description"];
                                    factorZone = factorZone.Remove(factorZone.LastIndexOf(")"));
                                    factorZone = factorZone.Substring(factorZone.LastIndexOf("C") + 1);
                                    string zone = factorZone[0] == '+' ? "%2B" : "%2D";
                                    factorZone = factorZone.Substring(1);
                                    string factorH = factorZone.Remove(factorZone.LastIndexOf(":"));
                                    string factorM = factorZone.Substring(factorZone.LastIndexOf(":") + 1);
                                    factorZ = zone + factorH + "%3A" + factorM;
                                }
                            }
                            catch
                            {
                                MessageBox.Show("Not a factor.", "IP unavailable.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            string strDate = date.Text;
                            string endDate = date.Text;
                            string str = "00";
                            string end = "01";

                            for (int i = 0; i < 24; i++)
                            {
                                str = i.ToString("00");
                                if (i < 23)
                                {
                                    end = (i + 1).ToString("00");
                                }
                                else
                                {
                                    DateTime newdate = DateTime.ParseExact(date.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                                    endDate = newdate.AddDays(1).ToString("yyyy-MM-dd");
                                    end = "00";
                                }
                                try
                                {
                                    string url = $"http://{ip.Text}/archive/tracks/count?dateStart={strDate}T{str}%3A00%3A00{factorZ}&dateEnd={endDate}T{end}%3A00%3A00{factorZ}";
                                    HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
                                    HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                                    using (StreamReader stream = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                                    {
                                        string factorJson = stream.ReadToEnd();
                                        var datajson = new JavaScriptSerializer().Deserialize<dynamic>(factorJson);
                                        dataGridView1.Rows[rowNumbe].Cells[1+i].Value = datajson["count"];
                                        progressBar1.PerformStep();
                                    }
                                }
                                catch
                                {
                                    MessageBox.Show("Factor request error not responding.", "Timed out.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    progressBar1.PerformStep();
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("The specified IP address is unavailable.", "IP unavailable.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Invalid IP address specified.", "Invalid IP.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("The date must be correct.", "Invalid Date.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("Invalid date format.", "Invalid Date.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            ip.Enabled = true;
            date.Enabled = true;
            request.Enabled = true;
            clear.Enabled = true;
            save.Enabled = true;
        }

        private void clear_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
        }

        private void save_Click(object sender, EventArgs e)
        {     
            dialog.IsFolderPicker = true;
            dialog.Multiselect = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                FileInfo fil = new FileInfo(dialog.FileName + "\\" + date.Text + ".csv");

                using (StreamWriter sw = fil.AppendText())
                {
                    var headers = dataGridView1.Columns.Cast<DataGridViewColumn>();
                    sw.WriteLine(string.Join(",", headers.Select(column => "\"" + column.HeaderText + "\"").ToArray()));
                    sw.Close();
                }
                using (StreamWriter sw = fil.AppendText())
                {
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        var cells = row.Cells.Cast<DataGridViewCell>();
                        sw.WriteLine(string.Join(",", cells.Select(cell => "\"" + cell.Value + "\"").ToArray()));
                    }                    
                    sw.Close();
                }
            }
        }
    }
}
