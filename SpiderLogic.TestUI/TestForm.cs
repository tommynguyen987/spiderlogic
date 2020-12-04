using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TextExtractor;

namespace TextExtractor.TestUI
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
        }

        private void GoButton_Click(object sender, EventArgs e)
        {
            SearchAndRefresh();
        }

        private void SearchAndRefresh()
        {
            try
            {
                StatusLabel.Text = "Searching...";
                //Application.DoEvents();

                UrlExtractor urlExtractor = new UrlExtractor();
                var urls = UrlExtractor.GetUrls(InputText.Text, IsRecursiveCheck.Checked);
                UrlList.Items.Clear();
                foreach (var url in urls)
                {
                    UrlList.Items.Add(url);
                    Application.DoEvents();
                }
                //UrlList.Items.AddRange(lists.ToArray());
                //Application.DoEvents();
                EmailList.Items.Clear();
                foreach (var item in urls)
                {
                    var emails = EmailExtractor.RetrieveEmails(item);                    
                    foreach (var email in emails)
                    {
                        EmailList.Items.Add(email);
                        Application.DoEvents();
                    }                    
                    //EmailList.Items.AddRange(emails.ToArray());
                }
            }
            finally 
            {
                StatusLabel.Text = string.Format("Found {0} url(s), {1} email(s)", UrlList.Items.Count, EmailList.Items.Count);
            }
        }

        private void TestForm_Load(object sender, EventArgs e)
        {

        }
    }
}
