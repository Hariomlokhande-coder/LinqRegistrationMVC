using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Configuration;
using LinqRegistrationMVC.Models;

namespace LinqRegistrationMVC.Controllers
{
    public class UserController : Controller
    {
        // ================= COMMON METHOD =================
        private List<User> GetAllUsers()
        {
            List<User> users = new List<User>();

            using (SqlConnection con = new SqlConnection(
                ConfigurationManager.ConnectionStrings["constr"].ToString()))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM Users", con);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    users.Add(new User
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        Name = dr["Name"].ToString(),
                        Email = dr["Email"].ToString(),
                        Password = dr["Password"].ToString()
                    });
                }
            }

            return users;
        }

        // ================= READ + LINQ =================
        public ActionResult Index(string search)
        {
            var users = GetAllUsers();

            // WHERE
            if (!string.IsNullOrEmpty(search))
            {
                users = users
                    .Where(u => u.Name.ToLower().Contains(search.ToLower()))
                    .ToList();
            }

            // ORDERBY
            users = users.OrderBy(u => u.Name).ToList();

            // COUNT
            ViewBag.TotalUsers = users.Count();

            // FIRSTORDEFAULT
            var firstUser = users.FirstOrDefault();
            ViewBag.FirstUserName = firstUser != null ? firstUser.Name : "No User";

            // ALL
            ViewBag.AllValidEmails = users.All(u => u.Email.Contains("@"));

            // ANY
            ViewBag.HasAdmin = users.Any(u => u.Name.ToLower() == "admin");

            // GROUPBY (Strongly Typed)
            var groupData = users
                .GroupBy(u => u.Email.Split('@')[1])
                .Select(g => new EmailGroup
                {
                    Domain = g.Key,
                    Total = g.Count()
                }).ToList();

            ViewBag.GroupData = groupData;

            // SELECT + TOUPPER
            var result = users
                .Select(u => new User
                {
                    Id = u.Id,
                    Name = u.Name.ToUpper(),
                    Email = u.Email,
                    Password = u.Password
                }).ToList();

            return View(result);
        }

        // ================= CREATE =================
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(User user)
        {
            // 🔥 VALIDATION CHECK
            if (!ModelState.IsValid)
                return View(user);

            var users = GetAllUsers();

            // ANY - Duplicate email
            if (users.Any(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View(user);
            }

            user.Name = user.Name.ToUpper();
            user.Email = user.Email.ToLower();

            using (SqlConnection con = new SqlConnection(
                ConfigurationManager.ConnectionStrings["constr"].ToString()))
            {
                string query = "INSERT INTO Users(Name,Email,Password) VALUES(@Name,@Email,@Password)";
                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Name", user.Name);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@Password", user.Password);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }

        // ================= EDIT =================
        public ActionResult Edit(int id)
        {
            var users = GetAllUsers();
            var user = users.SingleOrDefault(u => u.Id == id);//singleordefault use kiya yha par

            if (user == null)
                return RedirectToAction("Index");

            return View(user);
        }

        [HttpPost]
        public ActionResult Edit(User user)
        {
            // Validation check
            if (!ModelState.IsValid)
                return View(user);

            // Load existing users to check for duplicate emails
            var users = GetAllUsers();

            // Normalize email for comparison (ToLower().Trim())
            var normalizedEmail = (user.Email ?? string.Empty).ToLower().Trim();

            // Duplicate email check: allow same email for the same user id
            if (users.Any(u => ((u.Email ?? string.Empty).ToLower().Trim() == normalizedEmail) && u.Id != user.Id))
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View(user);
            }

            // Normalize values before update
            user.Name = (user.Name ?? string.Empty).ToUpper();
            user.Email = normalizedEmail;

            try
            {
                using (SqlConnection con = new SqlConnection(
                    ConfigurationManager.ConnectionStrings["constr"].ToString()))
                {
                    string query = "UPDATE Users SET Name=@Name, Email=@Email, Password=@Password WHERE Id=@Id";
                    SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@Id", user.Id);
                    cmd.Parameters.AddWithValue("@Name", user.Name);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Password", user.Password);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (SqlException)
            {
                // Return a model-level error (shown in ValidationSummary) and keep MVC flow
                ModelState.AddModelError("", "Unable to update user due to a database error. Please try again.");
                return View(user);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View(user);
            }

            return RedirectToAction("Index");
        }

        // ================= DELETE =================
        public ActionResult Delete(int id)
        {
            var users = GetAllUsers();
            var user = users.SingleOrDefault(u => u.Id == id);

            if (user != null)
            {
                using (SqlConnection con = new SqlConnection(
                    ConfigurationManager.ConnectionStrings["constr"].ToString()))
                {
                    string query = "DELETE FROM Users WHERE Id=@Id";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Id", id);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }
    }
}