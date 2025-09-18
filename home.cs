using ClosedXML.Excel;
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
    public partial class adminDashboard : Form
    {
        private dataAccess Da { get; set; }
        private DataSet Ds { get; set; }
        
        private string currentUserSearch = "";
        private string currentRoleFilter = "All";
        private string currentPaymentMethodFilter = "All";
        private string currentApplicationSearch = "";
        private string currentApplicationStatusFilter = "All";

        public adminDashboard()
        {
            InitializeComponent();
            this.Da = new dataAccess();
            this.dgvUsers.SelectionChanged += dgvUsers_SelectionChanged;
            this.cbFilterStatus.SelectedIndexChanged += cbFilterStatus_SelectedIndexChanged;
            this.cbFilterPaymentMethod.SelectedIndexChanged += cbFilterPaymentMethod_SelectedIndexChanged;
            this.btnDownloadPDF.Click += new System.EventHandler(this.btnDownloadPDF_Click);
            
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            login l = new login();
            l.Show();
            this.Hide();
        }

        // panel switching buttons
        private void btnViewUsers_Click(object sender, EventArgs e)
        {
            panelViewUsers.Visible = true;
            panelDashboard.Visible = false;
            panelEmployers.Visible = false;
            panelJobs.Visible = false;
            panelPayments.Visible = false;
            panelJobseekers.Visible = false;
            panelApplications.Visible = false;

            // Initialize filter controls
            cbFilterRole.SelectedItem = "All";
            tbId.Text = "";
            currentUserSearch = "";
            currentRoleFilter = "All";

            // 🔥 Clear setRoleBox selection
            cbSetRole.SelectedIndex = -1;

            // 🔴 REMOVE FOCUS from all controls
            this.ActiveControl = null;
            dgvUsers.ClearSelection();

            LoadUsers();
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            panelDashboard.Visible = true;
            panelViewUsers.Visible = false;
            panelEmployers.Visible = false;
            panelJobs.Visible = false;
            panelPayments.Visible = false;
            panelJobseekers.Visible = false;
            panelApplications.Visible = false;

            // Refresh dashboard statistics
            LoadDashboardStats();

            // 🔴 REMOVE FOCUS from all controls
            this.ActiveControl = null;
        }

        private void btnEmployers_Click(object sender, EventArgs e)
        {
            panelEmployers.Visible = true;
            panelDashboard.Visible = false;
            panelViewUsers.Visible = false;
            panelJobs.Visible = false;
            panelPayments.Visible = false;
            panelJobseekers.Visible = false;
            panelApplications.Visible = false;
            LoadEmployers();

            // 🔴 REMOVE FOCUS from all controls
            this.ActiveControl = null; // This removes focus from any control
            dgvEmployers.ClearSelection(); // Clear selection from DataGridView
        }

        private void btnJobs_Click(object sender, EventArgs e)
        {
            panelJobs.Visible = true;
            panelEmployers.Visible = false;
            panelDashboard.Visible = false;
            panelViewUsers.Visible = false;
            panelPayments.Visible = false;
            panelJobseekers.Visible = false;
            panelApplications.Visible = false;

            // ✅ Always set filter combobox to "All"
            cbFilterStatus.SelectedItem = "All";

            // 🔥 Clear setStatus selection
            cbSetStatus.SelectedIndex = -1;

            LoadJobs("All", ""); // Pass empty search keyword

            // 🔴 REMOVE FOCUS from all controls
            this.ActiveControl = null; // This removes focus from any control
            dgvJobs.ClearSelection(); // Clear selection from DataGridView
        }

        private void btnPayments_Click(object sender, EventArgs e)
        {
            panelPayments.Visible = true;
            panelJobs.Visible = false;
            panelEmployers.Visible = false;
            panelDashboard.Visible = false;
            panelViewUsers.Visible = false;
            panelJobseekers.Visible = false;
            panelApplications.Visible = false;

            // Initialize payment method filter
            cbFilterPaymentMethod.SelectedItem = "All";
            currentPaymentMethodFilter = "All";

            LoadPayments(); // ✅ load payment records

            // 🔴 REMOVE FOCUS from all controls
            this.ActiveControl = null; // This removes focus from any control
            dgvPayments.ClearSelection(); // Clear selection from DataGridView
        }

        private void btnJobSeekers_Click(object sender, EventArgs e)
        {
            panelJobseekers.Visible = true;
            panelDashboard.Visible = false;
            panelViewUsers.Visible = false;
            panelEmployers.Visible = false;
            panelJobs.Visible = false;
            panelPayments.Visible = false;
            panelApplications.Visible = false;
            LoadJobseekers();

            // 🔴 REMOVE FOCUS from all controls
            this.ActiveControl = null; // This removes focus from any control
            dgvJobseekers.ClearSelection(); // Clear selection from DataGridView
        }


        private void btnApplications_Click(object sender, EventArgs e)
        {
            panelApplications.Visible = true;
            panelJobseekers.Visible = false;
            panelDashboard.Visible = false;
            panelViewUsers.Visible = false;
            panelEmployers.Visible = false;
            panelJobs.Visible = false;
            panelPayments.Visible = false;

            // Initialize filter controls
            cbFilterApplicationStatus.SelectedItem = "All";
            tbSearchApplicationId.Text = "";
            currentApplicationSearch = "";
            currentApplicationStatusFilter = "All";

            LoadApplications();

            // 🔴 REMOVE FOCUS from all controls
            this.ActiveControl = null;
            dgvApplications.ClearSelection();
        }




        // payments panel
        private void cbFilterPaymentMethod_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbFilterPaymentMethod.SelectedItem != null)
            {
                currentPaymentMethodFilter = cbFilterPaymentMethod.SelectedItem.ToString();
                LoadPayments();
            }
        }


        private void btnDownloadPDF_Click(object sender, EventArgs e)
        {
            // DISABLE BUTTON IMMEDIATELY to prevent double-click
            btnDownloadPDF.Enabled = false;

            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";
                    saveFileDialog.FileName = $"Payments_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                    // Show dialog and check result
                    if (saveFileDialog.ShowDialog() != DialogResult.OK)
                    {
                        btnDownloadPDF.Enabled = true; // Re-enable if cancelled
                        return;
                    }

                    // Get the data from your DataGridView
                    DataTable dt = (DataTable)dgvPayments.DataSource;

                    if (dt == null || dt.Rows.Count == 0)
                    {
                        MessageBox.Show("No data to export!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        btnDownloadPDF.Enabled = true;
                        return;
                    }

                    // Create PDF document
                    Document document = new Document(PageSize.A4.Rotate());
                    PdfWriter.GetInstance(document, new FileStream(saveFileDialog.FileName, FileMode.Create));

                    document.Open();

                    // Add title
                    Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                    Paragraph title = new Paragraph("PAYMENTS REPORT", titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    title.SpacingAfter = 20f;
                    document.Add(title);

                    // Add generation date
                    Font dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                    Paragraph date = new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", dateFont);
                    date.Alignment = Element.ALIGN_RIGHT;
                    date.SpacingAfter = 15f;
                    document.Add(date);

                    // Create PDF table
                    PdfPTable pdfTable = new PdfPTable(dt.Columns.Count);
                    pdfTable.WidthPercentage = 100;
                    pdfTable.SpacingBefore = 10f;
                    pdfTable.SpacingAfter = 10f;

                    // Add column headers
                    Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                    foreach (DataColumn column in dt.Columns)
                    {
                        PdfPCell cell = new PdfPCell(new Phrase(column.ColumnName, headerFont));
                        cell.BackgroundColor = new BaseColor(200, 200, 200);
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        pdfTable.AddCell(cell);
                    }

                    // Add data rows
                    Font dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
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
                // ALWAYS RE-ENABLE BUTTON (even if error occurred)
                btnDownloadPDF.Enabled = true;
            }
        }

        private void btnDownloadExcelSheet_Click(object sender, EventArgs e)
        {
            // Disable button to prevent double-click
            btnDownloadExcelSheet.Enabled = false;

            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
                    saveFileDialog.FileName = $"Payments_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    if (saveFileDialog.ShowDialog() != DialogResult.OK)
                    {
                        btnDownloadExcelSheet.Enabled = true;
                        return;
                    }

                    // Get data from DataGridView
                    DataTable dt = (DataTable)dgvPayments.DataSource;

                    if (dt == null || dt.Rows.Count == 0)
                    {
                        MessageBox.Show("No data to export!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        btnDownloadExcelSheet.Enabled = true;
                        return;
                    }

                    // Create Excel workbook
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Payments");

                        // Add title
                        worksheet.Cell(1, 1).Value = "PAYMENTS REPORT";
                        worksheet.Range(1, 1, 1, dt.Columns.Count).Merge();
                        worksheet.Cell(1, 1).Style.Font.Bold = true;
                        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                        worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Add generation date
                        worksheet.Cell(2, 1).Value = $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                        worksheet.Range(2, 1, 2, dt.Columns.Count).Merge();
                        worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        worksheet.Cell(2, 1).Style.Font.Italic = true;

                        // Add column headers (start at row 4)
                        for (int i = 0; i < dt.Columns.Count; i++)
                        {
                            worksheet.Cell(4, i + 1).Value = dt.Columns[i].ColumnName;
                            worksheet.Cell(4, i + 1).Style.Font.Bold = true;
                            worksheet.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                            worksheet.Cell(4, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }

                        // Add data rows
                        for (int row = 0; row < dt.Rows.Count; row++)
                        {
                            for (int col = 0; col < dt.Columns.Count; col++)
                            {
                                worksheet.Cell(row + 5, col + 1).Value = dt.Rows[row][col]?.ToString();
                            }
                        }

                        // Auto-fit columns
                        worksheet.Columns().AdjustToContents();

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
                btnDownloadExcelSheet.Enabled = true;
            }
        }

        private void LoadPayments()
        {
            try
            {
                string sql = @"
            SELECT 
                p.paymentid,
                (u.firstName + ' ' + u.lastName + ' (' + p.employerid + ')') AS employer,
                p.jobid,
                p.paymentmethod,
                p.phonenumber,
                p.amount,
                j.publisheddate,
                j.publishedtime,
                p.paymentstatus
            FROM PaymentsTable p
            INNER JOIN EmployersTable e ON p.employerid = e.employerid
            INNER JOIN UsersTable u ON e.employerid = u.userID
            INNER JOIN JobsTable j ON p.jobid = j.jobid
            WHERE 1=1";

                // Apply payment method filter (only if not "All")
                if (currentPaymentMethodFilter != "All")
                {
                    sql += $" AND p.paymentmethod = '{currentPaymentMethodFilter}'";
                }

                DataSet ds = this.Da.ExecuteQuery(sql);

                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    dgvPayments.DataSource = null;
                    MessageBox.Show("No payment records found.");
                    return;
                }

                DataTable dt = ds.Tables[0];

                dgvPayments.AutoGenerateColumns = false;
                dgvPayments.Columns.Clear();

                dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "paymentid", HeaderText = "Payment ID", DataPropertyName = "paymentid" });
                dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "employer", HeaderText = "Paid By", DataPropertyName = "employer" });
                dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "jobid", HeaderText = "Job ID", DataPropertyName = "jobid" });
                dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "paymentmethod", HeaderText = "Payment Method", DataPropertyName = "paymentmethod" });
                dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "phonenumber", HeaderText = "Sender Phone", DataPropertyName = "phonenumber" });
                dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "amount", HeaderText = "Amount", DataPropertyName = "amount" });
                dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "publisheddate", HeaderText = "Payment Date", DataPropertyName = "publisheddate" });
                dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "publishedtime", HeaderText = "Payment Time", DataPropertyName = "publishedtime" });
                dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "paymentstatus", HeaderText = "Payment Status", DataPropertyName = "paymentstatus" });

                dgvPayments.DataSource = dt;

                // ✅ Fix column width and wrapping
                dgvPayments.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                dgvPayments.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                dgvPayments.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                // Clear selection after loading
                dgvPayments.ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading payments: " + ex.Message);
            }
        }



        // view users panel

        private void btnSearchUsers_Click(object sender, EventArgs e)
        {
            string searchId = tbId.Text?.Trim() ?? "";
            currentUserSearch = searchId;
            LoadUsers(currentRoleFilter, currentUserSearch);
        }

        private void cbFilterRole_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbFilterRole.SelectedItem != null)
            {
                currentRoleFilter = cbFilterRole.SelectedItem.ToString();
                LoadUsers(currentRoleFilter, currentUserSearch);
            }
        }

        private void btnUpdateRole_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvUsers.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a user first to update.");
                    return;
                }

                string currentId = dgvUsers.CurrentRow.Cells["userID"].Value.ToString();
                string currentRole = dgvUsers.CurrentRow.Cells["role"].Value.ToString();
                string newRole = cbSetRole.SelectedItem?.ToString();

                if (string.IsNullOrWhiteSpace(newRole))
                {
                    MessageBox.Show("Please select a role to update.");
                    return;
                }

                // 🔒 RESTRICTION: Only allow conversion from "Pending" to other roles
                // OR allow changing to the same role (no actual change)
                if (!currentRole.Equals("Pending", StringComparison.OrdinalIgnoreCase) &&
                    !currentRole.Equals(newRole, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Role conversion is only allowed for users with 'Pending' status.\n\n" +
                                  $"Current role: {currentRole}\n" +
                                  $"Selected role: {newRole}\n\n" +
                                  "You can only convert Pending users to other roles.",
                                  "Conversion Restricted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // If trying to set the same role, no need to update
                if (currentRole.Equals(newRole, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("User already has this role. No changes made.");
                    return;
                }

                // Decide prefix
                string newPrefix = "P";
                if (newRole.Equals("Employer", StringComparison.OrdinalIgnoreCase)) newPrefix = "E";
                else if (newRole.Equals("Admin", StringComparison.OrdinalIgnoreCase)) newPrefix = "A";
                else if (newRole.Equals("Job seeker", StringComparison.OrdinalIgnoreCase)) newPrefix = "JS";

                string numericPart = currentId.Contains("-") ? currentId.Split('-')[1] : currentId;
                string newUserId = $"{newPrefix}-{numericPart}";

                // If changing from Employer to something else → delete from EmployersTable first
                if (currentRole.Equals("Employer", StringComparison.OrdinalIgnoreCase) && newPrefix != "E")
                {
                    string deleteEmployer = $"DELETE FROM EmployersTable WHERE employerid='{currentId}';";
                    this.Da.ExecuteUpdateQuery(deleteEmployer);
                }

                // If changing from Job seeker to something else → delete from JobSeekersTable first
                if (currentRole.Equals("Job seeker", StringComparison.OrdinalIgnoreCase) && newPrefix != "JS")
                {
                    string deleteJobseeker = $"DELETE FROM JobSeekersTable WHERE jsid='{currentId}';";
                    this.Da.ExecuteUpdateQuery(deleteJobseeker);
                }

                // Update UsersTable
                string sqlUser = $@"
    UPDATE UsersTable
    SET role = '{newRole}', userID = '{newUserId}'
    WHERE userID = '{currentId}';
";
                int count = this.Da.ExecuteUpdateQuery(sqlUser);

                // If changed to Employer → ensure entry exists in EmployersTable
                if (newPrefix == "E")
                {
                    string checkEmployer = $"SELECT * FROM EmployersTable WHERE employerid = '{newUserId}'";
                    DataSet ds = this.Da.ExecuteQuery(checkEmployer);

                    if (ds == null || ds.Tables[0].Rows.Count == 0)
                    {
                        string insertEmployer = $@"
            INSERT INTO EmployersTable (employerid, companyName, companyPhone, website)
            VALUES ('{newUserId}', '', '', '');
        ";
                        this.Da.ExecuteUpdateQuery(insertEmployer);
                    }
                }

                // If changed to Job seeker → ensure entry exists in JobSeekersTable
                else if (newPrefix == "JS")
                {
                    string checkJobseeker = $"SELECT * FROM JobSeekersTable WHERE jsid = '{newUserId}'";
                    DataSet ds = this.Da.ExecuteQuery(checkJobseeker);

                    if (ds == null || ds.Tables[0].Rows.Count == 0)
                    {
                        string insertJobseeker = $@"
            INSERT INTO JobSeekersTable (jsid, dob, bloodgroup, nationality, maritalstatus, jsaddress, education)
            VALUES ('{newUserId}', '', '', '', '', '', '');
        ";
                        this.Da.ExecuteUpdateQuery(insertJobseeker);
                    }
                }

                if (count > 0)
                    MessageBox.Show($"Role updated successfully from {currentRole} to {newRole}.");
                else
                    MessageBox.Show("Role update failed.");

                LoadUsers();
                LoadEmployers();
                LoadJobseekers(); // Also reload jobseekers to reflect changes
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating role: " + ex.Message);
            }
        }

        private void dgvUsers_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvUsers.CurrentRow != null && dgvUsers.CurrentRow.Cells["role"].Value != null)
            {
                string role = dgvUsers.CurrentRow.Cells["role"].Value.ToString();

                // Ensure role exists in combo box
                if (cbSetRole.Items.Contains(role))
                {
                    cbSetRole.SelectedItem = role;
                }
                else
                {
                    cbSetRole.SelectedIndex = -1; // clear selection if not found
                }
            }
            else
            {
                // 🔥 Clear selection when no row is selected
                cbSetRole.SelectedIndex = -1;
            }
        }


        private void LoadUsers(string roleFilter = "All", string searchKeyword = "")
        {
            try
            {
                string query = "SELECT userID, firstName, lastName, gender, role, phone, email, password FROM UsersTable WHERE 1=1";

                // Apply role filter
                if (roleFilter != "All")
                {
                    query += $" AND role = '{roleFilter}'";
                }

                // Apply search filter
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    query += $" AND userID LIKE '%{searchKeyword}%'";
                }

                query += " ORDER BY userID";

                this.Ds = this.Da.ExecuteQuery(query);

                if (this.Ds != null && this.Ds.Tables.Count > 0)
                {
                    dgvUsers.AutoGenerateColumns = false;
                    dgvUsers.DataSource = this.Ds.Tables[0];
                    dgvUsers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                    dgvUsers.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    dgvUsers.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                    // Clear selection after loading
                    dgvUsers.ClearSelection();

                    // 🔥 Clear setRoleBox when data is reloaded
                    cbSetRole.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading users: " + ex.Message);
            }
        }














        // view jobs panel
        private void dgvJobs_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvJobs.CurrentRow != null && dgvJobs.Columns.Contains("status"))
            {
                if (dgvJobs.CurrentRow.Cells["status"].Value != null)
                {
                    string status = dgvJobs.CurrentRow.Cells["status"].Value.ToString();
                    if (cbSetStatus.Items.Contains(status))
                        cbSetStatus.SelectedItem = status;
                    else
                        cbSetStatus.SelectedIndex = -1;
                }
            }
            else
            {
                // 🔥 Clear selection when no row is selected
                cbSetStatus.SelectedIndex = -1;
            }
        }

        private void cbFilterStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedStatus = cbFilterStatus.SelectedItem?.ToString() ?? "All";
            string searchKeyword = tbSearchJobs.Text?.Trim() ?? "";
            LoadJobs(selectedStatus, searchKeyword);
        }

        private void btnSearchJobs_Click(object sender, EventArgs e)
        {
            string selectedStatus = cbFilterStatus.SelectedItem?.ToString() ?? "All";
            string searchKeyword = tbSearchJobs.Text?.Trim() ?? "";
            LoadJobs(selectedStatus, searchKeyword);
        }

        private void btnUpdateJob_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvJobs.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a job first.");
                    return;
                }

                string jobId = dgvJobs.CurrentRow.Cells["jobid"].Value.ToString();
                string currentStatus = dgvJobs.CurrentRow.Cells["status"].Value.ToString();
                string newStatus = cbSetStatus.SelectedItem?.ToString();

                if (string.IsNullOrWhiteSpace(newStatus))
                {
                    MessageBox.Show("Please select a status.");
                    return;
                }

                // If trying to set the same status, no need to update
                if (currentStatus.Equals(newStatus, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Job already has this status. No changes made.");
                    return;
                }

                string sql = $"UPDATE JobsTable SET status = '{newStatus}' WHERE jobid = '{jobId}'";
                int count = this.Da.ExecuteUpdateQuery(sql);

                if (count > 0)
                    MessageBox.Show($"Job status updated successfully from {currentStatus} to {newStatus}.");
                else
                    MessageBox.Show("Failed to update status.");

                // ✅ Reload with active filter or "All" if nothing selected
                string currentFilter = cbFilterStatus.SelectedItem?.ToString() ?? "All";
                LoadJobs(currentFilter);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating job: " + ex.Message);
            }
        }

        private void btnDeleteJobs_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvJobs.CurrentRow == null)
                {
                    MessageBox.Show("Please select a job first.");
                    return;
                }

                string jobId = dgvJobs.CurrentRow.Cells["jobid"].Value.ToString();

                var confirm = MessageBox.Show($"Delete job {jobId}?", "Confirm", MessageBoxButtons.YesNo);
                if (confirm != DialogResult.Yes) return;

                string sql = $"DELETE FROM JobsTable WHERE jobid = '{jobId}'";
                int count = this.Da.ExecuteUpdateQuery(sql);

                if (count > 0)
                    MessageBox.Show("Job deleted successfully.");
                else
                    MessageBox.Show("Failed to delete job.");

                LoadJobs(cbFilterStatus.SelectedItem?.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting job: " + ex.Message);
            }
        }

        

        private void LoadJobs(string statusFilter = "All", string searchKeyword = "")
        {
            try
            {
                string sql = @"
            SELECT 
                j.jobid,
                j.jobtitle,
                (u.firstName + ' ' + u.lastName + ' (' + j.employerid + ')') AS postedby,
                j.status AS jobstatus,   -- alias to avoid reserved keyword issues
                e.companyName AS company,
                j.employmenttype,
                j.salaryrange,
                j.experiencerequired,
                j.publisheddate,
                j.publishedtime
            FROM JobsTable j
            INNER JOIN EmployersTable e ON j.employerid = e.employerid
            INNER JOIN UsersTable u ON e.employerid = u.userID
            WHERE 1=1";

                // 🔎 Search filter
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    sql += " AND (j.jobtitle LIKE @keyword OR e.companyName LIKE @keyword)";
                }

                // 🔎 Status filter
                if (statusFilter != "All")
                {
                    sql += " AND j.status = @statusFilter";
                }

                SqlCommand cmd = new SqlCommand(sql, this.Da.Sqlcon);

                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    cmd.Parameters.AddWithValue("@keyword", "%" + searchKeyword + "%");
                }
                if (statusFilter != "All")
                {
                    cmd.Parameters.AddWithValue("@statusFilter", statusFilter);
                }

                DataSet ds = this.Da.ExecuteQuery(cmd);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    dgvJobs.AutoGenerateColumns = false;
                    dgvJobs.Columns.Clear();

                    // ✅ Define columns with correct DataPropertyNames
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "jobid", HeaderText = "Job ID", DataPropertyName = "jobid" });
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "jobtitle", HeaderText = "Job Title", DataPropertyName = "jobtitle" });
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "postedby", HeaderText = "Posted By", DataPropertyName = "postedby" });
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "status", HeaderText = "Status", DataPropertyName = "jobstatus" }); // ✅ Fixed: use jobstatus
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "company", HeaderText = "Company", DataPropertyName = "company" });
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "employmenttype", HeaderText = "Employment Type", DataPropertyName = "employmenttype" });
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "salaryrange", HeaderText = "Salary Range", DataPropertyName = "salaryrange" });
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "experiencerequired", HeaderText = "Experience", DataPropertyName = "experiencerequired" });
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "publisheddate", HeaderText = "Published Date", DataPropertyName = "publisheddate" });
                    dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "publishedtime", HeaderText = "Published Time", DataPropertyName = "publishedtime" });

                    dgvJobs.DataSource = ds.Tables[0];
                    // ✅ Fix column width and text wrapping
                    dgvJobs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                    dgvJobs.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    dgvJobs.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                    // 🔥 Clear selection after loading
                    dgvJobs.ClearSelection();

                    // 🔥 Clear setStatus when data is reloaded
                    cbSetStatus.SelectedIndex = -1;
                }
                else
                {
                    dgvJobs.DataSource = null;
                    MessageBox.Show("No jobs found with the current filters.");

                    // 🔥 Clear setStatus when no data
                    cbSetStatus.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading jobs: " + ex.Message);
            }
        }









        // employers panel
        private void btnInsertEmployer_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tbEmployerId.Text) ||
                    string.IsNullOrWhiteSpace(tbFirstEmployerName.Text) ||
                    string.IsNullOrWhiteSpace(tbLastEmployerName.Text) ||
                    string.IsNullOrWhiteSpace(tbEmployerCompany.Text) ||
                    string.IsNullOrWhiteSpace(tbEmployerPhone.Text) ||
                    string.IsNullOrWhiteSpace(tbEmployerEmail.Text))
                {
                    MessageBox.Show("All fields must be filled before inserting.");
                    return;
                }

                // Insert into UsersTable first
                string sqlUser = $@"
            INSERT INTO UsersTable (userID, firstName, lastName, email, phone, role)
            VALUES ('{tbEmployerId.Text}', '{tbFirstEmployerName.Text}', '{tbLastEmployerName.Text}', 
                    '{tbEmployerEmail.Text}', '{tbEmployerPhone.Text}', 'employer');";

                // Insert into EmployersTable
                string sqlEmployer = $@"
            INSERT INTO EmployersTable (employerid, companyName, companyPhone, website)
            VALUES ('{tbEmployerId.Text}', '{tbEmployerCompany.Text}', '{tbEmployerPhone.Text}', '{tbCompanyWebsite.Text}');";

                int count1 = this.Da.ExecuteUpdateQuery(sqlUser);
                int count2 = this.Da.ExecuteUpdateQuery(sqlEmployer);

                if (count1 == 1 && count2 == 1)
                    MessageBox.Show("Employer inserted successfully.");
                else
                    MessageBox.Show("Insertion failed.");

                LoadEmployers();
                ClearAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Insert error: " + ex.Message);
            }
        }

        private void btnUpdateEmployer_Click(object sender, EventArgs e)
        {
            try
            {
                string sqlUser = $@"
            UPDATE UsersTable
            SET firstName='{tbFirstEmployerName.Text}',
                lastName='{tbLastEmployerName.Text}',
                email='{tbEmployerEmail.Text}'
            WHERE userID='{tbEmployerId.Text}';";

                string sqlEmployer = $@"
            UPDATE EmployersTable
            SET companyName='{tbEmployerCompany.Text}',
                companyPhone='{tbEmployerPhone.Text}',
                website='{tbCompanyWebsite.Text}'
            WHERE employerid='{tbEmployerId.Text}';";

                int c1 = this.Da.ExecuteUpdateQuery(sqlUser);
                int c2 = this.Da.ExecuteUpdateQuery(sqlEmployer);

                if (c1 == 1 && c2 == 1)
                    MessageBox.Show("Employer updated successfully.");
                else
                    MessageBox.Show("Update failed.");

                LoadEmployers();
                ClearAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update error: " + ex.Message);
            }
        }

        private void btnDeleteEmployer_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvEmployers.CurrentRow == null) return;

                string empId = dgvEmployers.CurrentRow.Cells["employerid"].Value.ToString();
                string empName = dgvEmployers.CurrentRow.Cells["firstname"].Value.ToString();

                var confirm = MessageBox.Show($"Delete employer {empName}?", "Confirm", MessageBoxButtons.YesNo);
                if (confirm != DialogResult.Yes) return;

                string sqlEmployer = $"DELETE FROM EmployersTable WHERE employerid='{empId}';";
                string sqlUser = $"DELETE FROM UsersTable WHERE userID='{empId}';";

                this.Da.ExecuteUpdateQuery(sqlEmployer);
                this.Da.ExecuteUpdateQuery(sqlUser);

                MessageBox.Show("Employer deleted successfully.");

                LoadEmployers();
                ClearAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete error: " + ex.Message);
            }
        }

        private void dgvEmployers_DoubleClick(object sender, EventArgs e)
        {
            if (dgvEmployers.CurrentRow != null)
            {
                try
                {
                    this.tbEmployerId.Text = dgvEmployers.CurrentRow.Cells["employerid"].Value?.ToString();
                    this.tbFirstEmployerName.Text = dgvEmployers.CurrentRow.Cells["firstname"].Value?.ToString();
                    this.tbLastEmployerName.Text = dgvEmployers.CurrentRow.Cells["lastname"].Value?.ToString();
                    this.tbEmployerCompany.Text = dgvEmployers.CurrentRow.Cells["company"].Value?.ToString();
                    this.tbEmployerPhone.Text = dgvEmployers.CurrentRow.Cells["companyphone"].Value?.ToString();
                    this.tbEmployerEmail.Text = dgvEmployers.CurrentRow.Cells["emailaddress"].Value?.ToString();
                    this.tbCompanyWebsite.Text = dgvEmployers.CurrentRow.Cells["companywebsite"].Value?.ToString();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading employer details: " + ex.Message);
                }
            }
        }

        private void ClearAll()
        {
            this.tbSearchEmployer.Clear();
            this.tbEmployerId.Clear();
            this.tbFirstEmployerName.Clear();
            this.tbLastEmployerName.Clear();
            this.tbEmployerCompany.Clear();
            this.tbEmployerPhone.Clear();
            this.tbEmployerEmail.Clear();
            this.tbCompanyWebsite.Clear();
        }

        private void btnClearEmployer_Click(object sender, EventArgs e)
        {
            ClearAll();
        }


        // New method for loading Employers
        private void LoadEmployers()
        {
            try
            {
                string sql = @"
                    SELECT 
    u.userID        AS employerid,
    u.firstName     AS firstname,
    u.lastName      AS lastname,
    e.companyName   AS company,
    e.companyPhone  AS companyphone,   -- ONLY company phone
    u.Email         AS emailaddress,
    e.website       AS companywebsite
FROM UsersTable u
INNER JOIN EmployersTable e
    ON u.userID = e.employerid
WHERE LOWER(u.role) = 'employer';";

                DataSet ds = this.Da.ExecuteQuery(sql);

                if (ds == null || ds.Tables.Count == 0)
                {
                    dgvEmployers.DataSource = null;
                    MessageBox.Show("No employer records found.");
                    return;
                }

                DataTable dt = ds.Tables[0];

                dgvEmployers.AutoGenerateColumns = false;
                dgvEmployers.Columns.Clear();

                dgvEmployers.Columns.Add(new DataGridViewTextBoxColumn { Name = "employerid", HeaderText = "Employer ID", DataPropertyName = "employerid" });
                dgvEmployers.Columns.Add(new DataGridViewTextBoxColumn { Name = "firstname", HeaderText = "First name", DataPropertyName = "firstname" });
                dgvEmployers.Columns.Add(new DataGridViewTextBoxColumn { Name = "lastname", HeaderText = "Last name", DataPropertyName = "lastname" });
                dgvEmployers.Columns.Add(new DataGridViewTextBoxColumn { Name = "company", HeaderText = "Company", DataPropertyName = "company" });

                dgvEmployers.Columns.Add(new DataGridViewTextBoxColumn { Name = "companyphone", HeaderText = "Company Phone", DataPropertyName = "companyphone" });
                dgvEmployers.Columns.Add(new DataGridViewTextBoxColumn { Name = "emailaddress", HeaderText = "Email address", DataPropertyName = "emailaddress" });
                dgvEmployers.Columns.Add(new DataGridViewTextBoxColumn { Name = "companywebsite", HeaderText = "Company website", DataPropertyName = "companywebsite" });

                dgvEmployers.DataSource = dt;
                // ✅ Fix column width and text wrapping
                dgvEmployers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                dgvEmployers.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                dgvEmployers.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading employers: " + ex.Message);
            }
        }





        // applications panel
        // Method to load applications data
        private void LoadApplications(string statusFilter = "All", string searchKeyword = "")
        {
            try
            {
                string sql = @"
SELECT 
    a.applicationId,
    a.status AS applicationstatus,
    (j.jobtitle + ' (' + j.jobid + ')') AS job,
    (u.firstName + ' ' + u.lastName + ' (' + a.jsid + ')') AS jobseeker,
    j.salaryrange AS salaryrangeofjob,
    a.expectedSalary,
    a.applieddate,
    a.interviewDate,
    a.interviewTime
FROM ApplicationsTable a
INNER JOIN JobsTable j ON a.jobid = j.jobid
INNER JOIN UsersTable u ON a.jsid = u.userID
WHERE 1=1";

                // Apply status filter
                if (statusFilter != "All")
                {
                    sql += $" AND a.status = '{statusFilter}'";
                }

                // Apply search filter
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    sql += $" AND a.applicationId LIKE '%{searchKeyword}%'";
                }

                sql += " ORDER BY a.applicationId";

                DataSet ds = this.Da.ExecuteQuery(sql);

                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    dgvApplications.DataSource = null;
                    MessageBox.Show("No application records found with the current filters.");
                    return;
                }

                DataTable dt = ds.Tables[0];

                dgvApplications.AutoGenerateColumns = false;
                dgvApplications.Columns.Clear();

                dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "applicationId", HeaderText = "Application ID", DataPropertyName = "applicationId" });
                dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "applicationstatus", HeaderText = "Status", DataPropertyName = "applicationstatus" });
                dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "job", HeaderText = "Job", DataPropertyName = "job" });
                dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "jobseeker", HeaderText = "Jobseeker", DataPropertyName = "jobseeker" });
                dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "salaryrange", HeaderText = "Salary range", DataPropertyName = "salaryrangeofjob" });
                dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "expectedSalary", HeaderText = "Expected salary", DataPropertyName = "expectedSalary" });
                dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "applicationdate", HeaderText = "Applied date", DataPropertyName = "applieddate" });
                dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "interviewDate", HeaderText = "Interview date", DataPropertyName = "interviewDate" });
                dgvApplications.Columns.Add(new DataGridViewTextBoxColumn { Name = "interviewTime", HeaderText = "Interview time", DataPropertyName = "interviewTime" });

                dgvApplications.DataSource = dt;

                dgvApplications.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                dgvApplications.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                dgvApplications.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                // Clear selection after loading
                dgvApplications.ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading applications: " + ex.Message);
            }
        }

        private void btnViewApplication_Click(object sender, EventArgs e)
        {
            try
            {
                // Check if any row is actually selected (not just current row)
                if (dgvApplications.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select an application first by clicking on a row.");
                    return;
                }

                string applicationId = dgvApplications.CurrentRow.Cells["applicationId"].Value?.ToString();

                if (string.IsNullOrEmpty(applicationId))
                {
                    MessageBox.Show("No application ID found for the selected row.");
                    return;
                }

                // Open the view application form and pass the application ID
                viewApplicationByAdmin viewForm = new viewApplicationByAdmin(applicationId);
                viewForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening application details: " + ex.Message);
            }
        }

        private void btnSearchApplicationId_Click(object sender, EventArgs e)
        {
            string searchId = tbSearchApplicationId.Text?.Trim() ?? "";
            currentApplicationSearch = searchId;
            LoadApplications(currentApplicationStatusFilter, currentApplicationSearch);
        }

        private void cbFilterApplicationStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbFilterApplicationStatus.SelectedItem != null)
            {
                currentApplicationStatusFilter = cbFilterApplicationStatus.SelectedItem.ToString();
                LoadApplications(currentApplicationStatusFilter, currentApplicationSearch);
            }
        }



        private void btnDeleteApplication_Click(object sender, EventArgs e)
        {
            try
            {
                // Check if any row is actually selected (not just current row)
                if (dgvApplications.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select an application first by clicking on a row.");
                    return;
                }

                string applicationId = dgvApplications.CurrentRow.Cells["applicationId"].Value?.ToString();

                if (string.IsNullOrEmpty(applicationId))
                {
                    MessageBox.Show("No application ID found for the selected row.");
                    return;
                }

                string jobInfo = dgvApplications.CurrentRow.Cells["job"].Value?.ToString();
                string jobseekerInfo = dgvApplications.CurrentRow.Cells["jobseeker"].Value?.ToString();

                var confirm = MessageBox.Show($"Delete application {applicationId}?\nJob: {jobInfo}\nJobseeker: {jobseekerInfo}",
                            "Confirm Deletion",
                            MessageBoxButtons.YesNo);

                if (confirm != DialogResult.Yes) return;

                string deleteSql = $"DELETE FROM ApplicationsTable WHERE applicationId = '{applicationId}'";
                int result = this.Da.ExecuteUpdateQuery(deleteSql);

                if (result > 0)
                {
                    MessageBox.Show("Application deleted successfully.");
                    LoadApplications(currentApplicationStatusFilter, currentApplicationSearch); // Refresh with current filters
                }
                else
                {
                    MessageBox.Show("Failed to delete application.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting application: " + ex.Message);
            }
        }





        // jobseekers panel


        // Method to load jobseekers data
        private void LoadJobseekers()
        {
            try
            {
                string sql = @"
SELECT 
    j.jsid,
    u.firstName AS jsFirstName,
    u.lastName AS jsLastName,
    j.dob,
    j.bloodgroup,
    j.nationality,
    j.maritalstatus,
    j.jsaddress,
    j.education,
    (SELECT COUNT(*) FROM ApplicationsTable a WHERE a.jsid = j.jsid) AS applicationsno
FROM JobSeekersTable j
INNER JOIN UsersTable u ON j.jsid = u.userID
WHERE LOWER(u.role) = 'job seeker'";

                DataSet ds = this.Da.ExecuteQuery(sql);

                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    dgvJobseekers.DataSource = null;
                    MessageBox.Show("No jobseeker records found.");
                    return;
                }

                DataTable dt = ds.Tables[0];

                dgvJobseekers.AutoGenerateColumns = false;
                dgvJobseekers.Columns.Clear();

                dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "jsid", HeaderText = "Jobseeker ID", DataPropertyName = "jsid" });
                dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "jsFirstName", HeaderText = "First name", DataPropertyName = "jsFirstName" });
                dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "jsLastName", HeaderText = "Last name", DataPropertyName = "jsLastName" });
                dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "dob", HeaderText = "Date of birth", DataPropertyName = "dob" });
                dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "bloodgroup", HeaderText = "Blood group", DataPropertyName = "bloodgroup" });
                dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "nationality", HeaderText = "Nationality", DataPropertyName = "nationality" });
                dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "maritalstatus", HeaderText = "Marital status", DataPropertyName = "maritalstatus" });
                dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "jsaddress", HeaderText = "Address", DataPropertyName = "jsaddress" });
                dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "education", HeaderText = "Education", DataPropertyName = "education" });
                dgvJobseekers.Columns.Add(new DataGridViewTextBoxColumn { Name = "applicationsno", HeaderText = "Number of applications", DataPropertyName = "applicationsno" });

                dgvJobseekers.DataSource = dt;

                dgvJobseekers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                dgvJobseekers.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                dgvJobseekers.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading jobseekers: " + ex.Message);
            }
        }

        // Double-click event to populate form fields
        private void dgvJobseekers_DoubleClick(object sender, EventArgs e)
        {
            if (dgvJobseekers.CurrentRow != null)
            {
                try
                {
                    // Populate form fields with jobseeker data
                    tbJsId.Text = dgvJobseekers.CurrentRow.Cells["jsid"].Value?.ToString();
                    tbFirstNameJs.Text = dgvJobseekers.CurrentRow.Cells["jsFirstName"].Value?.ToString();
                    tbLastNameJs.Text = dgvJobseekers.CurrentRow.Cells["jsLastName"].Value?.ToString();
                    tbDateOfBirth.Text = dgvJobseekers.CurrentRow.Cells["dob"].Value?.ToString();
                    tbBloodGroup.Text = dgvJobseekers.CurrentRow.Cells["bloodgroup"].Value?.ToString();
                    tbNationality.Text = dgvJobseekers.CurrentRow.Cells["nationality"].Value?.ToString();
                    tbMaritalStatus.Text = dgvJobseekers.CurrentRow.Cells["maritalstatus"].Value?.ToString();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading jobseeker details: " + ex.Message);
                }
            }
        }

        private void btnInsertJobseekers_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tbJsId.Text) ||
                    string.IsNullOrWhiteSpace(tbFirstNameJs.Text) ||
                    string.IsNullOrWhiteSpace(tbLastNameJs.Text) ||
                    string.IsNullOrWhiteSpace(tbDateOfBirth.Text) ||
                    string.IsNullOrWhiteSpace(tbBloodGroup.Text) ||
                    string.IsNullOrWhiteSpace(tbNationality.Text) ||
                    string.IsNullOrWhiteSpace(tbMaritalStatus.Text))
                {
                    MessageBox.Show("All fields must be filled before inserting.");
                    return;
                }

                // Insert into UsersTable
                string sqlUser = $@"
INSERT INTO UsersTable (userID, firstName, lastName, role)
VALUES ('{tbJsId.Text}', '{tbFirstNameJs.Text}', '{tbLastNameJs.Text}', 'job seeker')";

                // Insert into JobSeekersTable with the 7 attributes
                string sqlJobseeker = $@"
INSERT INTO JobSeekersTable (jsid, dob, bloodgroup, nationality, maritalstatus)
VALUES ('{tbJsId.Text}', '{tbDateOfBirth.Text}', '{tbBloodGroup.Text}', 
        '{tbNationality.Text}', '{tbMaritalStatus.Text}')";

                int count1 = this.Da.ExecuteUpdateQuery(sqlUser);
                int count2 = this.Da.ExecuteUpdateQuery(sqlJobseeker);

                if (count1 == 1 && count2 == 1)
                    MessageBox.Show("Jobseeker inserted successfully.");
                else
                    MessageBox.Show("Insertion failed.");

                LoadJobseekers();
                ClearJobseekerFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Insert error: " + ex.Message);
            }
        }

        private void btnUpdateJobseeker_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tbJsId.Text))
                {
                    MessageBox.Show("Please select a jobseeker to update.");
                    return;
                }

                // Update UsersTable
                string sqlUser = $@"
UPDATE UsersTable
SET firstName='{tbFirstNameJs.Text}',
    lastName='{tbLastNameJs.Text}'
WHERE userID='{tbJsId.Text}'";

                // Update JobSeekersTable
                string sqlJobseeker = $@"
UPDATE JobSeekersTable
SET dob='{tbDateOfBirth.Text}',
    bloodgroup='{tbBloodGroup.Text}',
    nationality='{tbNationality.Text}',
    maritalstatus='{tbMaritalStatus.Text}'
WHERE jsid='{tbJsId.Text}'";

                int c1 = this.Da.ExecuteUpdateQuery(sqlUser);
                int c2 = this.Da.ExecuteUpdateQuery(sqlJobseeker);

                if (c1 == 1 && c2 == 1)
                    MessageBox.Show("Jobseeker updated successfully.");
                else
                    MessageBox.Show("Update failed.");

                LoadJobseekers();
                ClearJobseekerFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update error: " + ex.Message);
            }
        }

        private void btnDeleteJobseeker_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvJobseekers.CurrentRow == null)
                {
                    MessageBox.Show("Please select a jobseeker first.");
                    return;
                }

                string jsId = dgvJobseekers.CurrentRow.Cells["jsid"].Value?.ToString();
                string jsName = $"{dgvJobseekers.CurrentRow.Cells["jsFirstName"].Value} {dgvJobseekers.CurrentRow.Cells["jsLastName"].Value}";

                var confirm = MessageBox.Show($"Delete jobseeker {jsName}?", "Confirm", MessageBoxButtons.YesNo);
                if (confirm != DialogResult.Yes) return;

                // Delete related records first (applications, saved jobs, etc.)
                string deleteApplications = $"DELETE FROM ApplicationsTable WHERE jsid='{jsId}';";
                string deleteSavedJobs = $"DELETE FROM SavedJobsTable WHERE jsid='{jsId}';";

                // Delete from JobSeekersTable
                string deleteJobseeker = $"DELETE FROM JobSeekersTable WHERE jsid='{jsId}';";
                // Delete from UsersTable
                string deleteUser = $"DELETE FROM UsersTable WHERE userID='{jsId}';";

                // Execute in correct order (child tables first, then parent tables)
                this.Da.ExecuteUpdateQuery(deleteApplications);
                this.Da.ExecuteUpdateQuery(deleteSavedJobs);
                this.Da.ExecuteUpdateQuery(deleteJobseeker);
                this.Da.ExecuteUpdateQuery(deleteUser);

                MessageBox.Show("Jobseeker deleted successfully.");
                LoadJobseekers();
                ClearJobseekerFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete error: " + ex.Message);
            }
        }

        private void btnClearJobseeker_Click(object sender, EventArgs e)
        {
            ClearJobseekerFields();
        }

        private void ClearJobseekerFields()
        {
            tbJsId.Clear();
            tbFirstNameJs.Clear();
            tbLastNameJs.Clear();
            tbDateOfBirth.Clear();
            tbBloodGroup.Clear();
            tbNationality.Clear();
            tbMaritalStatus.Clear();
        }

        private void LoadDashboardStats()
        {
            try
            {
                // 1. Total User Accounts
                string sql1 = "SELECT COUNT(*) AS TotalUsers FROM UsersTable";
                DataSet ds1 = this.Da.ExecuteQuery(sql1);
                tbdashboard1.Text = ds1.Tables[0].Rows[0]["TotalUsers"] != DBNull.Value
                    ? ds1.Tables[0].Rows[0]["TotalUsers"].ToString() : "0";

                // 2. Users Awaiting Admin Verification
                string sql2 = "SELECT COUNT(*) AS PendingUsers FROM UsersTable WHERE Role = 'Pending'";
                DataSet ds2 = this.Da.ExecuteQuery(sql2);
                tbdashboard2.Text = ds2.Tables[0].Rows[0]["PendingUsers"] != DBNull.Value
                    ? ds2.Tables[0].Rows[0]["PendingUsers"].ToString() : "0";

                // 3. Jobs Created in the System
                string sql3 = "SELECT COUNT(*) AS TotalJobs FROM JobsTable";
                DataSet ds3 = this.Da.ExecuteQuery(sql3);
                tbdashboard3.Text = ds3.Tables[0].Rows[0]["TotalJobs"] != DBNull.Value
                    ? ds3.Tables[0].Rows[0]["TotalJobs"].ToString() : "0";

                // 4. Total Applications by Candidates
                string sql4 = "SELECT COUNT(*) AS TotalApplications FROM ApplicationsTable";
                DataSet ds4 = this.Da.ExecuteQuery(sql4);
                tbdashboard4.Text = ds4.Tables[0].Rows[0]["TotalApplications"] != DBNull.Value
                    ? ds4.Tables[0].Rows[0]["TotalApplications"].ToString() : "0";

                // 5. Postings Pending Admin Approval
                string sql5 = "SELECT COUNT(*) AS PendingJobs FROM JobsTable WHERE status = 'Pending'";
                DataSet ds5 = this.Da.ExecuteQuery(sql5);
                tbdashboard5.Text = ds5.Tables[0].Rows[0]["PendingJobs"] != DBNull.Value
                    ? ds5.Tables[0].Rows[0]["PendingJobs"].ToString() : "0";

                // 6. Applications Marked as Not Qualified
                string sql6 = @"SELECT COUNT(*) AS RejectedApplications FROM ApplicationsTable 
               WHERE status IN ('Rejected', 'Rejected after interview')";
                DataSet ds6 = this.Da.ExecuteQuery(sql6);
                tbdashboard6.Text = ds6.Tables[0].Rows[0]["RejectedApplications"] != DBNull.Value
                    ? ds6.Tables[0].Rows[0]["RejectedApplications"].ToString() : "0";

                // 7. Candidates Hired Through the Platform
                string sql7 = "SELECT COUNT(*) AS HiredCandidates FROM ApplicationsTable WHERE status = 'Hired'";
                DataSet ds7 = this.Da.ExecuteQuery(sql7);
                tbdashboard7.Text = ds7.Tables[0].Rows[0]["HiredCandidates"] != DBNull.Value
                    ? ds7.Tables[0].Rows[0]["HiredCandidates"].ToString() : "0";

                // 8. Earnings from Employer Job Postings
                string sql8 = "SELECT ISNULL(SUM(amount), 0) AS TotalEarnings FROM PaymentsTable WHERE paymentstatus = 'Completed'";
                DataSet ds8 = this.Da.ExecuteQuery(sql8);
                decimal earnings = ds8.Tables[0].Rows[0]["TotalEarnings"] != DBNull.Value
                    ? Convert.ToDecimal(ds8.Tables[0].Rows[0]["TotalEarnings"]) : 0;
                tbdashboard8.Text = earnings.ToString("0.00");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading dashboard statistics: " + ex.Message);

                // Set "0" values in case of error
                for (int i = 1; i <= 8; i++)
                {
                    var textBox = this.Controls.Find($"tbdashboard{i}", true).FirstOrDefault() as TextBox;
                    if (textBox != null)
                        textBox.Text = "0";
                }
            }
        }



        private void adminDashboard_Load(object sender, EventArgs e)
        {
            // Set up filter role combo box items
            if (cbFilterRole.Items.Count == 0)
            {
                cbFilterRole.Items.AddRange(new string[] { "All", "Admin", "Job seeker", "Employer", "Pending" });
                cbFilterRole.SelectedItem = "All";
                cbFilterRole.DropDownStyle = ComboBoxStyle.DropDownList;
            }

            // Set SetRoleBox to include ALL roles but start with no selection
            if (cbSetRole.Items.Count == 0)
            {
                cbSetRole.Items.AddRange(new string[] { "Admin", "Job seeker", "Employer", "Pending" });
                cbSetRole.DropDownStyle = ComboBoxStyle.DropDownList;
                cbSetRole.SelectedIndex = -1; // 🔥 NO DEFAULT SELECTION
            }

            // Set up filter status combo box for Jobs (with correct values)
            if (cbFilterStatus.Items.Count == 0)
            {
                cbFilterStatus.Items.AddRange(new string[] { "All", "Pending", "Active", "Expired", "Closed", "Rejected" });
                cbFilterStatus.SelectedItem = "All";
                cbFilterStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            }

            // Set SetStatus combo box for Jobs (with correct values)
            if (cbSetStatus.Items.Count == 0)
            {
                cbSetStatus.Items.AddRange(new string[] { "Pending", "Active", "Expired", "Closed", "Rejected" });
                cbSetStatus.DropDownStyle = ComboBoxStyle.DropDownList;
                cbSetStatus.SelectedIndex = -1; // 🔥 NO DEFAULT SELECTION
            }

            // Set up payment method filter combo box
            if (cbFilterPaymentMethod.Items.Count == 0)
            {
                cbFilterPaymentMethod.Items.AddRange(new string[] { "All", "Bkash", "Nagad", "Rocket" });
                cbFilterPaymentMethod.SelectedItem = "All";
                cbFilterPaymentMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            }

            // Set up application status filter combo box
            if (cbFilterApplicationStatus.Items.Count == 0)
            {
                cbFilterApplicationStatus.Items.AddRange(new string[] { "All", "Submitted", "Interview Scheduled", "Updated" });
                cbFilterApplicationStatus.SelectedItem = "All";
                cbFilterApplicationStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            }

            // Load dashboard statistics
            LoadDashboardStats();

            // Remove initial focus
            this.ActiveControl = null;

            // Load default Users
            LoadUsers();
        }

        


    }
}
