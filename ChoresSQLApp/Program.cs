using System;
using System.Data.SqlClient;
using System.Text;

namespace ChoresSQLApp
{
    class Program
    {
        static SqlConnection connection;
        static bool Exit = false;

        static void Main(string[] args)
        {            
            try
            { 
                Console.Write("Connecting to SQL Server... ");
                using (connection = new SqlConnection(LoginAndConnect()))
                {
                    connection.Open();
                   
                    InitializeDB();
                    MainLoop();
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }

        static void MainLoop()
        {
            do
            {
                var choice = MenuChoice();
                HandleChoice(choice);

            } while (Exit == false);

            Console.WriteLine("Exiting database. Press any key to continue...");
            Console.ReadKey(true);
        }

        static void HandleChoice(int choice)
        {
            switch(choice)
            {
                case 1:
                    Console.Write("What is the chore? ");
                    var chore = Console.ReadLine();
                    Console.Write("Who is it assigned to? ");
                    var person = Console.ReadLine();
                    CreateChore(chore, person);
                    break;
                case 2: // Handles input for UPDATE operation, as well as validating it.
                    { 
                        var input = "";

                        Console.WriteLine("Use this example to answer the below questions. Answers are case sensitive.");
                        Console.WriteLine("If SEARCH_FIELD is SEARCH_VALUE, change FIELD value to VALUE.\n");

                        while (input != "Chore" && input != "Assigned_To")
                        {
                            Console.Write("What is FIELD (Chore or Assigned_To)? ");
                            input = Console.ReadLine();
                            if (input != "Chore" && input != "Assigned_To")
                                Console.WriteLine("That is not a valid field.\n");
                        }
                        var field = input;

                        Console.Write($"What is VALUE (new value you are setting for {field})? ");
                        var value = Console.ReadLine();
                        
                        input = "";
                        while (input != "Id" && input != "Chore" && input != "Assigned_To")
                        {
                            Console.Write("What is SEARCH_FIELD (Id, Chore, or Assigned_To)? ");
                            input = Console.ReadLine();
                            if (input != "Id" && input != "Chore" && input != "Assigned_To")
                                Console.WriteLine("That is not a valid field.\n");
                        }
                        var searchField = input;

                        Console.Write($"What is SEARCH_VALUE (the value to find in order to set {field} to {value})? ");
                        var searchValue = Console.ReadLine();

                        UpdateChore(searchField, searchValue, field, value);
                        break;
                    }
                case 3:
                    ShowChores();
                    break;
                case 4:
                    Exit = true;
                    break;
            }
        }

        static int MenuChoice()
        {
            int choice;

            Console.WriteLine("Please select the operation you want to perform:");
            Console.WriteLine("1) Add new record to database.");
            Console.WriteLine("2) Update record in database.");
            Console.WriteLine("3) Show all database records.");
            Console.WriteLine("4) Exit database.\n");
            while (!Int32.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > 4)
            {
                Console.WriteLine("\nYou must choose a number 1 - 4: ");
            }
            Console.WriteLine();
            return choice;

        }

        static String LoginAndConnect()
        {
            SqlConnectionStringBuilder builder = new();
            builder.DataSource = "localhost";
            builder.UserID = "tabatson";
            builder.Password = "Password$4321";
            builder.InitialCatalog = "master";

            return builder.ConnectionString;
        }

        static void ExecuteSetup(String sqlCode)
        {
            using (SqlCommand command = new(sqlCode, connection))
            {
                int rows = command.ExecuteNonQuery();
                Console.WriteLine("Done. Press any key to continue...\n");
                Console.ReadKey(true);
            }
        }

        static void InitializeDB()
        {
            Console.WriteLine("Connected.\n");
            Console.WriteLine("Welcome to the Chores Assignment Database!\n");

            Console.WriteLine("Initializing Database...\n");
            ExecuteSetup(DropDB());
            ExecuteSetup(MakeDB());
            ExecuteSetup(MakeChoresTable());
            
            Console.WriteLine("Adding 3 default chores...\n");
            CreateChore("Wash Dishes", "Dad");
            CreateChore("Sweep Floor", "Mom");
            CreateChore("Put Away Toys", "Kids");

            Console.WriteLine("Performing initial table update...");
            UpdateChore("Assigned_To", "Kids", "Chore", "Clean Rooms");

            Console.WriteLine("Performing initial table deletion...");
            DeleteChore("Assigned_To", "Mom");
            
            ShowChores();

            Console.WriteLine("\nInitialization complete. Press any key to continue...\n");
            Console.ReadKey(true);
            Console.Clear();
        }

        static String DropDB()
        {
            Console.WriteLine("Dropping old Chores Database, if one exists...");
            return "DROP DATABASE IF EXISTS [ChoresDB];";
        }

        static String MakeDB()
        {
            Console.WriteLine("Creating new Chores Database...");
            return "CREATE DATABASE [ChoresDB];";
        }

        static String MakeChoresTable()
        {
            Console.WriteLine("Creating Chores Table with fields Id, Chore, Assigned_To");
            
            StringBuilder sb = new();
            sb.Append("USE ChoresDB; ");
            sb.Append("CREATE TABLE ChoreAssignments ( ");
            sb.Append(" Id INT IDENTITY(1,1) PRIMARY KEY, "); // Primary Key values must be unique by default.
            sb.Append(" Chore NVARCHAR(MAX), ");
            sb.Append(" Assigned_To NVARCHAR(MAX) ");
            sb.Append("); ");
            var sqlCode = sb.ToString();

            return sqlCode;
        }

        static void CreateChore(String chore, String person)
        {
            Console.Write($"Creating new chore {chore} assigned to {person}...");
            StringBuilder sb = new();
            sb.Append("INSERT ChoreAssignments (Chore, Assigned_To) ");
            sb.Append("VALUES (@Chore, @Assigned_To);");
            var sqlCode = sb.ToString();

            using (SqlCommand command = new(sqlCode, connection))
            {
                command.Parameters.AddWithValue("@Chore", chore);
                command.Parameters.AddWithValue("@Assigned_To", person);

                int rows = command.ExecuteNonQuery();
                Console.WriteLine(" Done.");
                Console.WriteLine($"{rows} rows added. Press any key to continue...\n");
                Console.ReadKey(true);
            }
        }

        static void UpdateChore(String searchField, String searchValue, String field, String value)
        {
            Console.Write($"Updating {field} to {value} where {searchField} is {searchValue}...");
            
            StringBuilder sb = new();            
            sb.Append($"UPDATE ChoreAssignments SET {field} = '{value}' WHERE {searchField} = @{searchField};");
            var sqlCode = sb.ToString();

            using (SqlCommand command = new(sqlCode, connection))
            {
                if (searchField == "Id" && Int32.TryParse(searchValue, out int id)) // Need the value to be an int if Id is the searchField
                    command.Parameters.AddWithValue($"@{searchField}", id);
                else
                    command.Parameters.AddWithValue($"@{searchField}", searchValue);

                int rows = command.ExecuteNonQuery();
                Console.WriteLine(" Done.");
                Console.WriteLine($"{rows} rows updated. Press any key to continue...\n");
                Console.ReadKey(true);
            }
        }

        static void DeleteChore(String searchField, String searchValue)
        {
            Console.Write($"Deleting records where {searchField} is {searchValue}...");

            var sqlCode = $"DELETE FROM ChoreAssignments WHERE {searchField} = @{searchField};";

            using (SqlCommand command = new(sqlCode, connection))
            {
                command.Parameters.AddWithValue($"@{searchField}", searchValue);

                int rows = command.ExecuteNonQuery();
                Console.WriteLine(" Done.");
                Console.WriteLine($"{rows} rows deleted. Press any key to continue...\n");
                Console.ReadKey(true);
            }
        }

        static void ShowChores()
        {
            Console.WriteLine("Showing list of chores...");

            var sqlCode = "SELECT * FROM ChoreAssignments;";

            using (SqlCommand command = new(sqlCode, connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    Console.WriteLine("\nId | Chore | Assigned_To");
                    Console.WriteLine("------------------------");
                    while (reader.Read())
                    {
                        Console.WriteLine("{0} | {1} | {2}", reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
                    }
                }
                Console.WriteLine("\nDone. Press any key to continue...\n");
                Console.ReadKey(true);
            }
        }
    }
}
