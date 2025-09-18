using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Employee_Management_System
{
    public partial class employerDashboard : Form
    {
        private readonly dataAccess da;
        private readonly string _userID;   // ✅ safer than Tag
        private string currentJobseekerSearch = "";
        

        // Strict constructor → userID must be passed
        public employerDashboard(string userID)
        {
            if (string.IsNullOrWhiteSpace(userID))
                throw new ArgumentException("EmployerDashboard requires a valid userID.", nameof(userID));

            InitializeComponent();

            // Initialize dataAccess FIRST
            da = new dataAccess();
            _userID = userID;

            // THEN set up event handlers
            btnSearchJob2.Click += btnSearchJob2_Click;
            btnClearSearchJob.Click += btnClearSearchJob_Click;
            tbSearchJobs2.KeyPress += tbSearchJobs2_KeyPress;
            btnSearchJob1.Click += btnSearchJob1_Click;
            btnUpdateJobStatus.Click += btnUpdateJobStatus_Click;
            tbSearchJobs1.KeyPress += tbSearchJobs1_KeyPress;

            // Set default filter values AFTER da is initialized
            if (cbFilterJobStatus != null && cbFilterJobStatus.Items.Count > 0)
            {
                // Temporarily remove the event handler to prevent immediate triggering
                cbFilterJobStatus.SelectedIndexChanged -= cbFilterJobStatus_SelectedIndexChanged;
                cbFilterJobStatus.SelectedIndex = 0;
                // Now add the event handler back
                cbFilterJobStatus.SelectedIndexChanged += cbFilterJobStatus_SelectedIndexChanged;
            }

            if (cbSetJobStatus != null && cbSetJobStatus.Items.Count > 0)
                cbSetJobStatus.SelectedIndex = 0;

            this.Load += employerDashboard_Load;
            ForcePasswordMask();
            tbAmountUsd.ReadOnly = true;
        }

        private void LoadEmployerDashboardStats()
        {
            try
            {
                // 1. Total jobs currently listed (posted by this specific employer)
                string sql1 = $"SELECT COUNT(*) AS TotalJobs FROM JobsTable WHERE employerid = '{_userID}'";
                DataSet ds1 = da.ExecuteQuery(sql1);
                tbdashboard1.Text = ds1.Tables[0].Rows[0]["TotalJobs"] != DBNull.Value
                    ? ds1.Tables[0].Rows[0]["TotalJobs"].ToString() : "0";

                // 2. Jobs still open for applications (this employer's jobs with status "Active")
                string sql2 = $"SELECT COUNT(*) AS ActiveJobs FROM JobsTable WHERE employerid = '{_userID}' AND status = 'Active'";
                DataSet ds2 = da.ExecuteQuery(sql2);
                tbdashboard2.Text = ds2.Tables[0].Rows[0]["ActiveJobs"] != DBNull.Value
                    ? ds2.Tables[0].Rows[0]["ActiveJobs"].ToString() : "0";

                // 3. Jobs that are expired or filled (this employer's jobs with status "Closed" and "Expired")
                string sql3 = $"SELECT COUNT(*) AS ClosedExpiredJobs FROM JobsTable WHERE employerid = '{_userID}' AND status IN ('Closed', 'Expired')";
                DataSet ds3 = da.ExecuteQuery(sql3);
                tbdashboard3.Text = ds3.Tables[0].Rows[0]["ClosedExpiredJobs"] != DBNull.Value
                    ? ds3.Tables[0].Rows[0]["ClosedExpiredJobs"].ToString() : "0";

                // 4. Total applications across all jobs (total applications for all jobs posted by this employer)
                string sql4 = $@"
SELECT COUNT(*) AS TotalApplications 
FROM ApplicationsTable a
INNER JOIN JobsTable j ON a.jobid = j.jobid
WHERE j.employerid = '{_userID}'";
                DataSet ds4 = da.ExecuteQuery(sql4);
                tbdashboard4.Text = ds4.Tables[0].Rows[0]["TotalApplications"] != DBNull.Value
                    ? ds4.Tables[0].Rows[0]["TotalApplications"].ToString() : "0";

                // 5. Applications awaiting employer action (applications for this employer's jobs with status "Submitted")
                string sql5 = $@"
SELECT COUNT(*) AS SubmittedApplications 
FROM ApplicationsTable a
INNER JOIN JobsTable j ON a.jobid = j.jobid
WHERE j.employerid = '{_userID}' AND a.status = 'Submitted'";
                DataSet ds5 = da.ExecuteQuery(sql5);
                tbdashboard5.Text = ds5.Tables[0].Rows[0]["SubmittedApplications"] != DBNull.Value
                    ? ds5.Tables[0].Rows[0]["SubmittedApplications"].ToString() : "0";

                // 6. Interviews arranged with candidates (applications for this employer's jobs with status "Interview Scheduled")
                string sql6 = $@"
SELECT COUNT(*) AS InterviewScheduled 
FROM ApplicationsTable a
INNER JOIN JobsTable j ON a.jobid = j.jobid
WHERE j.employerid = '{_userID}' AND a.status = 'Interview Scheduled'";
                DataSet ds6 = da.ExecuteQuery(sql6);
                tbdashboard6.Text = ds6.Tables[0].Rows[0]["InterviewScheduled"] != DBNull.Value
                    ? ds6.Tables[0].Rows[0]["InterviewScheduled"].ToString() : "0";

                // 7. Candidates who were not selected (applications for this employer's jobs with status "Rejected" and "Rejected after interview")
                string sql7 = $@"
SELECT COUNT(*) AS RejectedApplications 
FROM ApplicationsTable a
INNER JOIN JobsTable j ON a.jobid = j.jobid
WHERE j.employerid = '{_userID}' AND a.status IN ('Rejected', 'Rejected after interview')";
                DataSet ds7 = da.ExecuteQuery(sql7);
                tbdashboard7.Text = ds7.Tables[0].Rows[0]["RejectedApplications"] != DBNull.Value
                    ? ds7.Tables[0].Rows[0]["RejectedApplications"].ToString() : "0";

                // 8. Candidates successfully recruited (applications for this employer's jobs with status "Hired")
                string sql8 = $@"
SELECT COUNT(*) AS HiredCandidates 
FROM ApplicationsTable a
INNER JOIN JobsTable j ON a.jobid = j.jobid
WHERE j.employerid = '{_userID}' AND a.status = 'Hired'";
                DataSet ds8 = da.ExecuteQuery(sql8);
                tbdashboard8.Text = ds8.Tables[0].Rows[0]["HiredCandidates"] != DBNull.Value
                    ? ds8.Tables[0].Rows[0]["HiredCandidates"].ToString() : "0";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading employer dashboard statistics: " + ex.Message);

                // Set "0" values in case of error
                for (int i = 1; i <= 8; i++)
                {
                    var textBox = this.Controls.Find($"tbdashboard{i}", true).FirstOrDefault() as TextBox;
                    if (textBox != null)
                        textBox.Text = "0";
                }
            }
        }



        private void employerDashboard_Load(object sender, EventArgs e)
        {
            if (IsProfileComplete(_userID))
            {
                panelDashboardEmployer2.Visible = true;
                panelDashboardEmployer1.Visible = false;
                panelEmployerJobPost.Visible = false;
                panelJobseekers.Visible = false;
                panelJobs.Visible = false;

                // Load employer dashboard statistics
                LoadEmployerDashboardStats();
            }
            else
            {
                panelDashboardEmployer1.Visible = true;
                panelDashboardEmployer2.Visible = false;
                panelEmployerJobPost.Visible = false;
                panelJobseekers.Visible = false;
                panelJobs.Visible = false;
            }
        }

        private void RemoveFocusFromDataGridViews()
        {
            // Clear selection from all DataGridView controls
            
            dgvJobs.ClearSelection();
            dgvJobseekers.ClearSelection();

            // Remove focus from DataGridView controls
            
            dgvJobs.CurrentCell = null;
            dgvJobseekers.CurrentCell = null;

            // Set TabStop to false to prevent focus
            
            dgvJobs.TabStop = false;
            dgvJobseekers.TabStop = false;
        }



        private void ForcePasswordMask()
        {
            tbEnterPin.UseSystemPasswordChar = false;
            tbEnterPin.PasswordChar = '*';
        }

        // ✅ Reusable helper method
        private bool IsProfileComplete(string userID)
        {
            try
            {
                string sql = $@"
SELECT u.gender,
       e.companyname, e.companyphone, e.website, e.industry, e.address, e.about
FROM UsersTable u
LEFT JOIN EmployersTable e ON u.userID = e.employerid
WHERE u.userID = '{userID}'";

                DataTable dt = da.ExecuteQueryTable(sql);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    return !string.IsNullOrWhiteSpace(row["gender"].ToString()) &&
                           !string.IsNullOrWhiteSpace(row["companyname"].ToString()) &&
                           !string.IsNullOrWhiteSpace(row["companyphone"].ToString()) &&
                           !string.IsNullOrWhiteSpace(row["website"].ToString()) &&
                           !string.IsNullOrWhiteSpace(row["industry"].ToString()) &&
                           !string.IsNullOrWhiteSpace(row["address"].ToString()) &&
                           !string.IsNullOrWhiteSpace(row["about"].ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error checking profile completeness: " + ex.Message,
                                "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }

        



        // panel switching buttons
        private void btnDashboard_Click(object sender, EventArgs e)
        {
            if (IsProfileComplete(_userID))
            {
                panelDashboardEmployer2.Visible = true;
                panelDashboardEmployer1.Visible = false;
                panelEmployerJobPost.Visible = false;
                panelJobseekers.Visible = false;
                panelJobs.Visible = false;
                panelApplications.Visible = false;

                // Refresh employer dashboard statistics when dashboard is clicked
                LoadEmployerDashboardStats();
            }
            else
            {
                panelDashboardEmployer1.Visible = true;
                panelDashboardEmployer2.Visible = false;
                panelEmployerJobPost.Visible = false;
                panelJobseekers.Visible = false;
                panelJobs.Visible = false;
                panelApplications.Visible = false;
            }
        }

        private void btnApplications_Click(object sender, EventArgs e)
        {
            panelApplications.Visible = true;
            panelDashboardEmployer1.Visible = false;
            panelDashboardEmployer2.Visible = false;
            panelEmployerJobPost.Visible = false;
            panelJobseekers.Visible = false;
            panelJobs.Visible = false;

            // Initialize the filter combobox to show "All"
            InitializeApplicationFilter();
            LoadApplications();
        }

        private void btnJobSeekers_Click(object sender, EventArgs e)
        {
            panelJobseekers.Visible = true;
            panelDashboardEmployer1.Visible = false;
            panelDashboardEmployer2.Visible = false;
            panelEmployerJobPost.Visible = false;
            panelJobs.Visible = false;
            panelApplications.Visible = false;

            // Clear search and load all jobseekers
            tbSearchJobs2.Clear();
            currentJobseekerSearch = "";
            LoadJobseekersForEmployer(""); // Load jobseekers who applied to this employer's jobs
            RemoveFocusFromDataGridViews();
        }

        private void btnJobs_Click(object sender, EventArgs e)
        {
            panelJobs.Visible = true;
            panelDashboardEmployer1.Visible = false;
            panelDashboardEmployer2.Visible = false;
            panelEmployerJobPost.Visible = false;
            panelJobseekers.Visible = false;
            panelApplications.Visible = false;

            // Load with default parameters
            LoadJobsForEmployer("", "All");
            RemoveFocusFromDataGridViews();
        }



        private void btnLogout_Click(object sender, EventArgs e)
        {
            login l = new login();
            l.Show();
            this.Hide();
        }

        private void btnPostJob_Click(object sender, EventArgs e)
        {
            if (IsProfileComplete(_userID))
            {
                panelEmployerJobPost.Visible = true;
                panelDashboardEmployer1.Visible = false;
                panelDashboardEmployer2.Visible = false;
                panelJobseekers.Visible = false;

            }
            else
            {
                MessageBox.Show("You cannot post a job without completing your profile details.",
                                "Incomplete Profile",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
            }
        }




        // Post a job panel
        private void btnContinue_Click(object sender, EventArgs e)
        {
            string gender = cbGender.Text.Trim();
            string cname = tbCompanyName.Text.Trim();
            string cphone = tbCompanyPhone.Text.Trim();
            string csite = tbCompanyWebsite.Text.Trim();
            string industry = tbCompanyIndustry.Text.Trim();
            string caddr = tbCompanyAddress.Text.Trim();
            string about = tbCompanyDesc.Text.Trim();

            if (string.IsNullOrWhiteSpace(gender) ||
                string.IsNullOrWhiteSpace(cname) ||
                string.IsNullOrWhiteSpace(cphone) ||
                string.IsNullOrWhiteSpace(csite) ||
                string.IsNullOrWhiteSpace(industry) ||
                string.IsNullOrWhiteSpace(caddr) ||
                string.IsNullOrWhiteSpace(about))
            {
                MessageBox.Show("All fields are required.", "Validation Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string esc(string s) => s?.Replace("'", "''");

            try
            {
                // Update gender in UsersTable
                string sqlUser = $@"
UPDATE UsersTable
SET gender = '{esc(gender)}'
WHERE userID = '{esc(_userID)}';";

                int affectedUser = da.ExecuteUpdateQuery(sqlUser);
                if (affectedUser == 0)
                {
                    MessageBox.Show("User not found. Please make sure the account exists/was approved.",
                                    "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Upsert into EmployersTable
                string sqlEmployer = $@"
IF EXISTS (SELECT 1 FROM EmployersTable WHERE employerid = '{esc(_userID)}')
BEGIN
    UPDATE EmployersTable
    SET companyname    = '{esc(cname)}',
        companyphone   = '{esc(cphone)}',
        website        = '{esc(csite)}',
        industry       = '{esc(industry)}',
        address        = '{esc(caddr)}',
        about          = '{esc(about)}'
    WHERE employerid = '{esc(_userID)}';
END
ELSE
BEGIN
    INSERT INTO EmployersTable
        (employerid, companyname, companyphone, website, industry, address, about)
    VALUES
        ('{esc(_userID)}', '{esc(cname)}', '{esc(cphone)}', '{esc(csite)}',
         '{esc(industry)}', '{esc(caddr)}', '{esc(about)}');
END";

                int affectedEmp = da.ExecuteUpdateQuery(sqlEmployer);

                if (affectedEmp >= 0)
                {
                    MessageBox.Show("Profile saved successfully. You can proceed to post a job.",
                                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    panelDashboardEmployer2.Visible = true;
                    panelDashboardEmployer1.Visible = false;
                    panelEmployerJobPost.Visible = false;
                }
                else
                {
                    MessageBox.Show("Could not save company information. Please try again.",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database error: " + ex.Message,
                                "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            cbGender.SelectedIndex = -1;
            tbCompanyName.Clear();
            tbCompanyPhone.Clear();
            tbCompanyWebsite.Clear();
            tbCompanyIndustry.Clear();
            tbCompanyAddress.Clear();
            tbCompanyDesc.Clear();
            cbGender.Focus();
        }

        

        private void btnSubmitForm_Click(object sender, EventArgs e)
        {
            string jobTitle = tbJobTitle.Text.Trim();
            string employmentType = cbEmploymentType.Text.Trim();
            string experience = tbExperienceRequired.Text.Trim();
            string salaryRange = tbSalaryRange.Text.Trim();
            string deadline = tbDeadline.Text.Trim();
            string phone = tbEnterSendingNumber.Text.Trim();
            string amount = tbAmountUsd.Text.Trim();
            string pin = tbEnterPin.Text.Trim();

            string paymentMethod = "";
            if (rbBkash.Checked) paymentMethod = "Bkash";
            else if (rbRocket.Checked) paymentMethod = "Rocket";
            else if (rbNagad.Checked) paymentMethod = "Nagad";

            // ✅ Basic Empty Validation
            if (string.IsNullOrWhiteSpace(jobTitle) ||
                string.IsNullOrWhiteSpace(employmentType) ||
                string.IsNullOrWhiteSpace(experience) ||
                string.IsNullOrWhiteSpace(salaryRange) ||
                string.IsNullOrWhiteSpace(deadline) ||
                string.IsNullOrWhiteSpace(paymentMethod) ||
                string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(amount) ||
                string.IsNullOrWhiteSpace(pin))
            {
                MessageBox.Show("All fields are required.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (pin.Length < 4)
            {
                MessageBox.Show("Enter a valid PIN (minimum 4 digits).", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ✅ Salary Range Validation (Format: $60000-$70000 USD)
            string salaryPattern = @"^\$\d+-\$\d+ USD$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(salaryRange, salaryPattern))
            {
                MessageBox.Show("Salary range must be in format: \"$60000-$70000 USD\"",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ✅ Deadline Validation (Format: 30 Oct, 2025)
            string deadlinePattern = @"^\d{1,2} [A-Z][a-z]{2}, \d{4}$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(deadline, deadlinePattern))
            {
                MessageBox.Show("Deadline must be in format: \"30 Oct, 2025\"",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string esc(string s) => s?.Replace("'", "''");

                // ✅ Generate next JobID
                string sqlMaxJob = "SELECT ISNULL(MAX(CAST(SUBSTRING(jobid, 3, LEN(jobid)-2) AS INT)), 100) FROM JobsTable";
                int maxJobId = Convert.ToInt32(da.ExecuteScalarQuery(sqlMaxJob));
                string jobId = "J-" + (maxJobId + 1);

                // ✅ Generate next PaymentID
                string sqlMaxPay = "SELECT ISNULL(MAX(CAST(SUBSTRING(paymentid, 4, LEN(paymentid)-3) AS INT)), 200) FROM PaymentsTable";
                int maxPayId = Convert.ToInt32(da.ExecuteScalarQuery(sqlMaxPay));
                string paymentId = "PM-" + (maxPayId + 1);

                // ✅ Format Date and Time
                string formattedDate = DateTime.Now.ToString("dd MMM, yyyy"); // Example: 10 Aug, 2025
                string formattedTime = DateTime.Now.ToString("h:mm tt");      // Example: 9:50 AM

                // ✅ Insert into JobsTable
                string sqlJob = $@"
INSERT INTO JobsTable 
    (jobid, employerid, jobtitle, employmenttype, experiencerequired, salaryrange, deadline, publisheddate, publishedtime, status)
VALUES 
    ('{esc(jobId)}', '{esc(_userID)}', '{esc(jobTitle)}', '{esc(employmentType)}', 
     '{esc(experience)}', '{esc(salaryRange)}', '{esc(deadline)}', 
     '{esc(formattedDate)}', '{esc(formattedTime)}', 'Active')";

                int jobResult = da.ExecuteUpdateQuery(sqlJob);

                // ✅ Insert into PaymentsTable
                string sqlPayment = $@"
INSERT INTO PaymentsTable (paymentid, employerid, jobid, paymentmethod, phonenumber, amount, paymentstatus)
VALUES ('{esc(paymentId)}', '{esc(_userID)}', '{esc(jobId)}',
        '{esc(paymentMethod)}', '{esc(phone)}', '{esc(amount)}', 'Completed')";

                int payResult = da.ExecuteUpdateQuery(sqlPayment);

                if (jobResult > 0 && payResult > 0)
                {
                    MessageBox.Show("Job posted successfully with payment recorded!",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Clear form
                    tbJobTitle.Clear();
                    cbEmploymentType.SelectedIndex = -1;
                    tbExperienceRequired.Clear();
                    tbSalaryRange.Clear();
                    tbDeadline.Clear();
                    tbEnterSendingNumber.Clear();
                    tbEnterPin.Clear();
                    rbBkash.Checked = rbRocket.Checked = rbNagad.Checked = false;
                }
                else
                {
                    MessageBox.Show("Failed to save job or payment.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database error: " + ex.Message,
                    "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }






        // applications panel
        private void InitializeApplicationFilter()
        {
            if (cbFilterApplicationStatus != null)
            {
                // Temporarily remove event handler to prevent triggering during initialization
                cbFilterApplicationStatus.SelectedIndexChanged -= cbFilterApplicationStatus_SelectedIndexChanged;

                // Ensure "All" is selected
                for (int i = 0; i < cbFilterApplicationStatus.Items.Count; i++)
                {
                    if (cbFilterApplicationStatus.Items[i].ToString() == "All")
                    {
                        cbFilterApplicationStatus.SelectedIndex = i;
                        break;
                    }
                }

                // If "All" not found, select first item
                if (cbFilterApplicationStatus.SelectedIndex == -1 && cbFilterApplicationStatus.Items.Count > 0)
                {
                    cbFilterApplicationStatus.SelectedIndex = 0;
                }

                // Add event handler back
                cbFilterApplicationStatus.SelectedIndexChanged += cbFilterApplicationStatus_SelectedIndexChanged;
            }
        }

        public void LoadApplications(string searchKeyword = "", string filterStatus = "All")
        {
            try
            {
                string sql = @"
SELECT DISTINCT
    j.jobTitle AS job,
    a.applicationId AS applicationid,
    (u.firstName + ' ' + u.lastName) AS jobseeker,
    j.salaryRange AS salrange,
    a.expectedSalary AS expectedsal,
    a.appliedDate AS applieddate,
    a.interviewDate AS interviewdate,
    a.interviewTime AS interviewtime,
    a.status AS applicationstatus
FROM ApplicationsTable a
INNER JOIN JobsTable j ON a.jobid = j.jobid
INNER JOIN JobSeekersTable js ON a.jsid = js.jsid
INNER JOIN UsersTable u ON js.jsid = u.userID
WHERE j.employerId = @employerID";

                // Add search filter if keyword is provided
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    sql += @" AND (j.jobtitle LIKE @searchKeyword OR 
                          u.firstName LIKE @searchKeyword OR 
                          u.lastName LIKE @searchKeyword OR 
                          a.status LIKE @searchKeyword)";
                }

                // Add status filter if not "All"
                if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "All")
                {
                    sql += @" AND a.status = @filterStatus";
                }

                SqlCommand cmd = new SqlCommand(sql, da.Sqlcon);
                cmd.Parameters.AddWithValue("@employerID", _userID);

                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    cmd.Parameters.AddWithValue("@searchKeyword", "%" + searchKeyword + "%");
                }

                if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "All")
                {
                    cmd.Parameters.AddWithValue("@filterStatus", filterStatus);
                }

                DataSet ds = da.ExecuteQuery(cmd);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    dgvApplications.AutoGenerateColumns = false;
                    dgvApplications.Columns.Clear();

                    dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "applicationid", HeaderText = "Application ID", DataPropertyName = "applicationid", Visible = false });
                    dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "job", HeaderText = "Job title", DataPropertyName = "job" });
                    dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "applicationstatus", HeaderText = "Application status", DataPropertyName = "applicationstatus" });
                    dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "jobseeker", HeaderText = "Candidate", DataPropertyName = "jobseeker" });
                    dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "salrange", HeaderText = "Salary range", DataPropertyName = "salrange" });
                    dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "expectedsal", HeaderText = "Expected salary", DataPropertyName = "expectedsal" });
                    dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "applieddate", HeaderText = "Applied date", DataPropertyName = "applieddate" });
                    dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "interviewdate", HeaderText = "Interview date", DataPropertyName = "interviewdate" });
                    dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "interviewtime", HeaderText = "Interview time", DataPropertyName = "interviewtime" });

                    dgvApplications.DataSource = ds.Tables[0];

                    dgvApplications.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                    dgvApplications.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    dgvApplications.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                    dgvApplications.ClearSelection();
                    dgvApplications.CurrentCell = null;
                }
                else
                {
                    dgvApplications.DataSource = null;
                    ClearApplicationSelection();

                    if (!string.IsNullOrEmpty(searchKeyword) || (filterStatus != "All" && !string.IsNullOrEmpty(filterStatus)))
                    {
                        string filterMessage = "";
                        if (!string.IsNullOrEmpty(searchKeyword) && filterStatus != "All")
                        {
                            filterMessage = $"No applications found matching '{searchKeyword}' with status '{filterStatus}'.";
                        }
                        else if (!string.IsNullOrEmpty(searchKeyword))
                        {
                            filterMessage = $"No applications found matching '{searchKeyword}'.";
                        }
                        else
                        {
                            filterMessage = $"No applications found with status '{filterStatus}'.";
                        }

                        MessageBox.Show(filterMessage, "No Results",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("No applications found for your jobs.", "No Applications",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading applications: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                ClearApplicationSelection();
            }
        }

        private void ClearApplicationSelection()
        {
            if (dgvApplications != null)
            {
                dgvApplications.ClearSelection();
                dgvApplications.CurrentCell = null;
                dgvApplications.TabStop = false;
            }
        }


        private void btnViewApplicationForm_Click(object sender, EventArgs e)
        {
            if (dgvApplications.CurrentRow == null)
            {
                MessageBox.Show("Please select an application first.", "Selection Required",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Get the application ID from the selected row
            string applicationId = dgvApplications.CurrentRow.Cells["applicationid"]?.Value?.ToString();

            if (string.IsNullOrEmpty(applicationId))
            {
                MessageBox.Show("Could not retrieve application ID.", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Open the view application form and pass the parent reference
            viewApplicationByEmployer viewForm = new viewApplicationByEmployer(applicationId);
            viewForm.ParentDashboard = this; // Pass reference to this dashboard
            viewForm.ShowDialog();

            // Clear selection AFTER the form is closed
            ClearApplicationSelection();
        }

        private void dgvApplications_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // Ensure a valid row is clicked, not the header
            {
                // You can optionally highlight the selected row or store the application ID
                dgvApplications.Rows[e.RowIndex].Selected = true;
            }

            if (e.RowIndex >= 0 && e.RowIndex < dgvApplications.Rows.Count)
            {
                string currentStatus = dgvApplications.Rows[e.RowIndex].Cells["applicationstatus"]?.Value?.ToString();

                if (!string.IsNullOrEmpty(currentStatus))
                {
                    // Try to find and select the current status in the combobox
                    for (int i = 0; i < cbSetApplicationStatus.Items.Count; i++)
                    {
                        if (cbSetApplicationStatus.Items[i].ToString() == currentStatus)
                        {
                            cbSetApplicationStatus.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        private void btnSearchJob_Click(object sender, EventArgs e)
        {
            string searchKeyword = tbSearchJob.Text?.Trim() ?? "";
            string filterStatus = cbFilterApplicationStatus?.SelectedItem?.ToString() ?? "All";

            LoadApplications(searchKeyword, filterStatus);
        }

        private void btnClearSearch_Click(object sender, EventArgs e)
        {
            tbSearchJob.Clear();

            // Reset filter to "All"
            if (cbFilterApplicationStatus != null)
            {
                // Temporarily remove event handler to prevent immediate triggering
                cbFilterApplicationStatus.SelectedIndexChanged -= cbFilterApplicationStatus_SelectedIndexChanged;

                // Find and select "All" item
                for (int i = 0; i < cbFilterApplicationStatus.Items.Count; i++)
                {
                    if (cbFilterApplicationStatus.Items[i].ToString() == "All")
                    {
                        cbFilterApplicationStatus.SelectedIndex = i;
                        break;
                    }
                }

                // Add event handler back
                cbFilterApplicationStatus.SelectedIndexChanged += cbFilterApplicationStatus_SelectedIndexChanged;
            }

            LoadApplications("", "All"); // Load all applications
            ClearApplicationSelection(); // Clear any selection
        }

        private void tbSearchJob_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true; // Prevent the beep sound
                btnSearchJob_Click(sender, e);
            }
        }


        private void btnUpdateApplicationStatus_Click(object sender, EventArgs e)
        {
            // Check if a row is selected
            if (dgvApplications.CurrentRow == null)
            {
                MessageBox.Show("Please select an application first.", "Selection Required",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Get the current status from the selected row
            string currentStatus = dgvApplications.CurrentRow.Cells["applicationstatus"]?.Value?.ToString();

            // Get the selected status from the combobox
            string selectedStatus = cbSetApplicationStatus.SelectedItem?.ToString();

            // Validate if a status is selected
            if (string.IsNullOrEmpty(selectedStatus))
            {
                MessageBox.Show("Please select a status from the dropdown.", "Status Required",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if the status is actually being changed
            if (currentStatus == selectedStatus)
            {
                MessageBox.Show("No update has been made. The selected status is the same as the current status.",
                                "No Changes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Get the application ID from the selected row
            string applicationId = dgvApplications.CurrentRow.Cells["applicationid"]?.Value?.ToString();

            if (string.IsNullOrEmpty(applicationId))
            {
                MessageBox.Show("Could not retrieve application ID.", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Update the application status in the database
                string sql = $@"
UPDATE ApplicationsTable 
SET status = '{selectedStatus.Replace("'", "''")}'
WHERE applicationId = '{applicationId.Replace("'", "''")}'";

                int result = da.ExecuteUpdateQuery(sql);

                if (result > 0)
                {
                    MessageBox.Show($"Application status updated to '{selectedStatus}' successfully!",
                                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Refresh the applications grid to show the updated status
                    LoadApplications();

                    // Clear the combobox selection
                    cbSetApplicationStatus.SelectedIndex = -1;

                    // Clear selection and focus
                    ClearApplicationSelection();
                }
                else
                {
                    MessageBox.Show("Failed to update application status.", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating application status: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cbFilterApplicationStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            string searchKeyword = tbSearchJob?.Text?.Trim() ?? "";
            string filterStatus = cbFilterApplicationStatus?.SelectedItem?.ToString() ?? "All";

            LoadApplications(searchKeyword, filterStatus);
        }













        // jobs panel
        private void btnSearchJob2_Click(object sender, EventArgs e)
        {
            string searchKeyword = tbSearchJobs2.Text?.Trim() ?? "";
            currentJobseekerSearch = searchKeyword;
            LoadJobseekersForEmployer(currentJobseekerSearch);
        }

        private void btnClearSearchJob_Click(object sender, EventArgs e)
        {
            tbSearchJobs2.Clear();
            currentJobseekerSearch = "";
            LoadJobseekersForEmployer(""); // Load all jobseekers
        }

        private void tbSearchJobs2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true; // Prevent the beep sound
                btnSearchJob2_Click(sender, e);
            }
        }

        private void btnSearchJob1_Click(object sender, EventArgs e)
        {
            string searchKeyword = tbSearchJobs1?.Text?.Trim() ?? "";
            string filterStatus = (cbFilterJobStatus?.SelectedItem?.ToString()) ?? "All";

            LoadJobsForEmployer(searchKeyword, filterStatus);
        }

        private void btnUpdateJobStatus_Click(object sender, EventArgs e)
        {
            if (dgvJobs.CurrentRow == null)
            {
                MessageBox.Show("Please select a job first.");
                return;
            }

            string selectedStatus = cbSetJobStatus.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(selectedStatus))
            {
                MessageBox.Show("Please select a status to set.");
                return;
            }

            string jobTitle = dgvJobs.CurrentRow.Cells["JobTitle"]?.Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(jobTitle))
            {
                MessageBox.Show("Could not identify the selected job.");
                return;
            }

            try
            {
                string sql = $@"
UPDATE JobsTable 
SET status = '{selectedStatus}'
WHERE jobtitle = '{jobTitle.Replace("'", "''")}' 
AND employerid = '{_userID}'";

                int result = da.ExecuteUpdateQuery(sql);
                if (result > 0)
                {
                    MessageBox.Show($"Job status updated to '{selectedStatus}' successfully!");

                    // Get current search and filter values
                    string searchKeyword = tbSearchJobs1.Text?.Trim() ?? "";
                    string filterStatus = cbFilterJobStatus.SelectedItem?.ToString() ?? "All";

                    // ✅ TEMPORARILY remove event handler to prevent duplicate firing
                    cbFilterJobStatus.SelectedIndexChanged -= cbFilterJobStatus_SelectedIndexChanged;
                    LoadJobsForEmployer(searchKeyword, filterStatus);
                    // ✅ Add event handler back after loading
                    //cbFilterJobStatus.SelectedIndexChanged += cbFilterJobStatus_SelectedIndexChanged;

                    // ✅ CLEAR SELECTION AND REMOVE FOCUS
                    dgvJobs.ClearSelection();
                    dgvJobs.CurrentCell = null;
                }
                else
                {
                    MessageBox.Show("Failed to update job status.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating job status: " + ex.Message);
                // ✅ Ensure event handler is added back even on error
                cbFilterJobStatus.SelectedIndexChanged += cbFilterJobStatus_SelectedIndexChanged;
            }
        }

        private void cbFilterJobStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            string searchKeyword = tbSearchJobs1?.Text?.Trim() ?? "";
            string filterStatus = (cbFilterJobStatus?.SelectedItem?.ToString()) ?? "All";

            LoadJobsForEmployer(searchKeyword, filterStatus);
        }

        private void tbSearchJobs1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                btnSearchJob1_Click(sender, e);
            }
        }
        private void LoadJobsForEmployer(string searchKeyword = "", string filterStatus = "All")
        {
            try
            {
                string sql = @"
SELECT 
    j.jobtitle AS JobTitle,
    j.status AS Status,
    e.industry AS Industry,
    e.companyname AS Company,
    j.salaryrange AS SalaryRange,
    j.experiencerequired AS ExperienceRequired,
    j.deadline AS Deadline,
    j.publisheddate AS PublishedDate,
    j.publishedtime AS PublishedTime,
    COUNT(a.applicationId) AS NumberOfApplications
FROM JobsTable j
INNER JOIN EmployersTable e ON j.employerid = e.employerid
LEFT JOIN ApplicationsTable a ON j.jobid = a.jobid
WHERE j.employerid = @employerID";

                // Add search filter if keyword is provided
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    sql += @" AND j.jobtitle LIKE @searchKeyword";
                }

                // Add status filter if not "All"
                if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "All")
                {
                    sql += @" AND j.status = @filterStatus";
                }

                sql += @"
GROUP BY j.jobtitle, j.status, e.industry, e.companyname, 
         j.salaryrange, j.experiencerequired, j.deadline,
         j.publisheddate, j.publishedtime
ORDER BY j.publisheddate DESC, j.publishedtime DESC";

                SqlCommand cmd = new SqlCommand(sql, da.Sqlcon);
                cmd.Parameters.AddWithValue("@employerID", _userID);

                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    cmd.Parameters.AddWithValue("@searchKeyword", "%" + searchKeyword + "%");
                }

                if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "All")
                {
                    cmd.Parameters.AddWithValue("@filterStatus", filterStatus);
                }

                DataSet ds = da.ExecuteQuery(cmd);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    dgvJobs.AutoGenerateColumns = false;
                    dgvJobs.Columns.Clear();

                    // Add columns with proper DataPropertyNames
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "JobTitle", HeaderText = "Job title", DataPropertyName = "JobTitle" });
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status" });
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Industry", HeaderText = "Industry", DataPropertyName = "Industry" });
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Company", HeaderText = "Company", DataPropertyName = "Company" });
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalaryRange", HeaderText = "Salary range", DataPropertyName = "SalaryRange" });
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "expRequired", HeaderText = "Experience required", DataPropertyName = "ExperienceRequired" });
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Deadline", HeaderText = "Deadline", DataPropertyName = "Deadline" });
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "PublishedDate", HeaderText = "Published date", DataPropertyName = "PublishedDate" });
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "PublishedTime", HeaderText = "Published time", DataPropertyName = "PublishedTime" });
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "NumberOfApplications", HeaderText = "Number of applications", DataPropertyName = "NumberOfApplications" });

                    dgvJobs.DataSource = ds.Tables[0];

                    // Fix column width and text wrapping
                    dgvJobs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                    dgvJobs.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    dgvJobs.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                }
                else
                {
                    if (dgvJobs != null)
                        dgvJobs.DataSource = null;

                    // ✅ FIXED: Only show message for active searches, not initial load
                    bool isInitialLoad = string.IsNullOrEmpty(searchKeyword) && filterStatus == "All";
                    bool hasActiveSearch = !string.IsNullOrEmpty(searchKeyword) ||
                                          (filterStatus != "All" && !string.IsNullOrEmpty(filterStatus));

                    if (hasActiveSearch && !isInitialLoad)
                    {
                        MessageBox.Show("No jobs found matching your criteria.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading jobs: " + ex.Message);
            }
        }










        // jobseekers panel

        private void LoadJobseekersForEmployer(string searchKeyword = "")
        {
            try
            {
                string sql = @"
SELECT DISTINCT
    j.jobtitle AS JobTitle,
    u.firstName AS firstname,
    u.lastName AS lastname,
    u.gender AS gender,
    js.bloodgroup AS bloodgroup,
    js.nationality AS nationality,
    js.maritalstatus AS maritalstatus,
    js.jsaddress AS address,
    u.phone AS jsphone,
    js.dob AS dob,
    u.email AS emailaddress
FROM JobSeekersTable js
INNER JOIN UsersTable u ON js.jsid = u.userID
INNER JOIN ApplicationsTable a ON js.jsid = a.jsid
INNER JOIN JobsTable j ON a.jobid = j.jobid
WHERE j.employerid = @employerID";

                // Add job title search filter if keyword is provided
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    sql += @" AND j.jobtitle LIKE @searchKeyword";
                }

                SqlCommand cmd = new SqlCommand(sql, da.Sqlcon);
                cmd.Parameters.AddWithValue("@employerID", _userID);

                // Add search parameter if keyword is provided
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    cmd.Parameters.AddWithValue("@searchKeyword", "%" + searchKeyword + "%");
                }

                DataSet ds = da.ExecuteQuery(cmd);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    dgvJobseekers.AutoGenerateColumns = false;
                    dgvJobseekers.Columns.Clear();

                    // Add columns with proper DataPropertyNames
                    dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "JobTitle", HeaderText = "Job Title", DataPropertyName = "JobTitle" });
                    dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Firstname", HeaderText = "First name", DataPropertyName = "firstname" });
                    dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "lastname", HeaderText = "Last name", DataPropertyName = "lastname" });
                    dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "gender", HeaderText = "Gender", DataPropertyName = "gender" });
                    dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "bloodgroup", HeaderText = "Bloodgroup", DataPropertyName = "bloodgroup" });
                    dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "nationality", HeaderText = "Nationality", DataPropertyName = "nationality" });
                    dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "maritalstatus", HeaderText = "Marital status", DataPropertyName = "maritalstatus" });
                    dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "address", HeaderText = "Address", DataPropertyName = "address" });
                    dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "jsphone", HeaderText = "Phone", DataPropertyName = "jsphone" });
                    dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "dob", HeaderText = "Date of birth", DataPropertyName = "dob" });
                    dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "emailaddress", HeaderText = "Email address", DataPropertyName = "emailaddress" });

                    dgvJobseekers.DataSource = ds.Tables[0];

                    // Fix column width and text wrapping
                    dgvJobseekers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                    dgvJobseekers.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    dgvJobseekers.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                    // Clear selection and remove focus
                    dgvJobseekers.ClearSelection();
                    dgvJobseekers.CurrentCell = null;
                }
                else
                {
                    dgvJobseekers.DataSource = null;
                    if (!string.IsNullOrEmpty(searchKeyword))
                    {
                        MessageBox.Show($"No jobseekers found for job title containing '{searchKeyword}'.");
                    }
                    else
                    {
                        MessageBox.Show("No jobseekers found who applied to your jobs.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading jobseekers: " + ex.Message);
            }
        }

        private void btnDownloadPDF_Click(object sender, EventArgs e)
        {
            // Disable button to prevent double-click
            btnDownloadPDFcandidates.Enabled = false;

            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";
                    saveFileDialog.FileName = $"Jobseekers_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                    if (saveFileDialog.ShowDialog() != DialogResult.OK)
                    {
                        btnDownloadPDFcandidates.Enabled = true;
                        return;
                    }

                    DataTable dt = (DataTable)dgvJobseekers.DataSource;

                    if (dt == null || dt.Rows.Count == 0)
                    {
                        MessageBox.Show("No data to export!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        btnDownloadPDFcandidates.Enabled = true;
                        return;
                    }

                    // Create PDF document
                    Document document = new Document(PageSize.A4.Rotate());
                    PdfWriter.GetInstance(document, new FileStream(saveFileDialog.FileName, FileMode.Create));

                    document.Open();

                    // Add title
                    iTextSharp.text.Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                    Paragraph title = new Paragraph("JOBSEEKERS REPORT", titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    title.SpacingAfter = 20f;
                    document.Add(title);

                    // Add employer info
                    iTextSharp.text.Font infoFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                    Paragraph info = new Paragraph($"Employer: {_userID}\nGenerated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", infoFont);
                    info.Alignment = Element.ALIGN_RIGHT;
                    info.SpacingAfter = 15f;
                    document.Add(info);

                    // Create PDF table
                    PdfPTable pdfTable = new PdfPTable(dt.Columns.Count);
                    pdfTable.WidthPercentage = 100;
                    pdfTable.SpacingBefore = 10f;
                    pdfTable.SpacingAfter = 10f;

                    // Add column headers
                    iTextSharp.text.Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                    foreach (DataColumn column in dt.Columns)
                    {
                        PdfPCell cell = new PdfPCell(new Phrase(column.ColumnName, headerFont));
                        cell.BackgroundColor = new BaseColor(200, 200, 200);
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        pdfTable.AddCell(cell);
                    }

                    // Add data rows
                    iTextSharp.text.Font dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
                    foreach (DataRow row in dt.Rows)
                    {
                        foreach (DataColumn column in dt.Columns)
                        {
                            string cellValue = row[column]?.ToString() ?? "";
                            PdfPCell cell = new PdfPCell(new Phrase(cellValue, dataFont));
                            cell.HorizontalAlignment = Element.ALIGN_LEFT;
                            pdfTable.AddCell(cell);
                        }
                    }

                    document.Add(pdfTable);
                    document.Close();

                    MessageBox.Show($"PDF exported successfully!\nSaved as: {saveFileDialog.FileName}",
                                  "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting PDF: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnDownloadPDFcandidates.Enabled = true;
            }
        }

        private void btnDownloadExcelSheet_Click(object sender, EventArgs e)
        {
            // Disable button to prevent double-click
            btnDownloadExcelSheetCandidates.Enabled = false;

            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
                    saveFileDialog.FileName = $"Jobseekers_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    if (saveFileDialog.ShowDialog() != DialogResult.OK)
                    {
                        btnDownloadExcelSheetCandidates.Enabled = true;
                        return;
                    }

                    // Get data from DataGridView
                    DataTable dt = (DataTable)dgvJobseekers.DataSource;

                    if (dt == null || dt.Rows.Count == 0)
                    {
                        MessageBox.Show("No data to export!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        btnDownloadExcelSheetCandidates.Enabled = true;
                        return;
                    }

                    // Create Excel workbook
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Jobseekers");

                        // Add title
                        worksheet.Cell(1, 1).Value = "JOBSEEKERS REPORT";
                        worksheet.Range(1, 1, 1, dt.Columns.Count).Merge();
                        worksheet.Cell(1, 1).Style.Font.Bold = true;
                        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                        worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Add employer info
                        worksheet.Cell(2, 1).Value = $"Employer: {_userID}";
                        worksheet.Range(2, 1, 2, dt.Columns.Count).Merge();
                        worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        worksheet.Cell(2, 1).Style.Font.Italic = true;

                        // Add generation date
                        worksheet.Cell(3, 1).Value = $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                        worksheet.Range(3, 1, 3, dt.Columns.Count).Merge();
                        worksheet.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        worksheet.Cell(3, 1).Style.Font.Italic = true;

                        // Add column headers (start at row 5)
                        for (int i = 0; i < dt.Columns.Count; i++)
                        {
                            worksheet.Cell(5, i + 1).Value = dt.Columns[i].ColumnName;
                            worksheet.Cell(5, i + 1).Style.Font.Bold = true;
                            worksheet.Cell(5, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                            worksheet.Cell(5, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            worksheet.Cell(5, i + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        }

                        // Add data rows (start at row 6)
                        for (int row = 0; row < dt.Rows.Count; row++)
                        {
                            for (int col = 0; col < dt.Columns.Count; col++)
                            {
                                worksheet.Cell(row + 6, col + 1).Value = dt.Rows[row][col]?.ToString();
                                worksheet.Cell(row + 6, col + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            }
                        }

                        // Auto-fit columns
                        worksheet.Columns().AdjustToContents();

                        // Add borders to the entire data range
                        var dataRange = worksheet.Range(5, 1, dt.Rows.Count + 5, dt.Columns.Count);
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                        // Save workbook
                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    MessageBox.Show($"Excel file exported successfully!\nSaved as: {saveFileDialog.FileName}",
                                  "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting Excel: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnDownloadExcelSheetCandidates.Enabled = true;
            }
        }

        

        private void btnClearSearchJob2_Click(object sender, EventArgs e)
        {
            // ✅ TEMPORARILY remove event handler
            cbFilterJobStatus.SelectedIndexChanged -= cbFilterJobStatus_SelectedIndexChanged;

            // Clear search textbox
            if (tbSearchJobs1 != null)
                tbSearchJobs1.Clear();

            // Reset filter to "All" safely
            if (cbFilterJobStatus != null)
            {
                // Find "All" item or set to first item
                for (int i = 0; i < cbFilterJobStatus.Items.Count; i++)
                {
                    if (cbFilterJobStatus.Items[i].ToString() == "All")
                    {
                        cbFilterJobStatus.SelectedIndex = i;
                        break;
                    }
                }

                // If "All" not found, select first item
                if (cbFilterJobStatus.SelectedIndex == -1 && cbFilterJobStatus.Items.Count > 0)
                    cbFilterJobStatus.SelectedIndex = 0;
            }

            // Reload all jobs
            LoadJobsForEmployer("", "All");

            // ✅ Add event handler back
            cbFilterJobStatus.SelectedIndexChanged += cbFilterJobStatus_SelectedIndexChanged;
        }

        
    }
}
