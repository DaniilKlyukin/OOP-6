using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace ScooterRental.Controllers;

public class RentalController : Controller
{
    // GET: Главная страница аренды
    [HttpGet]
    public IActionResult Index()
    {
        // Прямой SQL-запрос для получения списка самокатов
        SqliteConnection connection = new SqliteConnection("Data Source=scooters.db");

        try
        {
            connection.Open();

            // Создаем таблицы, если их нет
            SqliteCommand createTablesCmd = new SqliteCommand(@"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Phone TEXT,
                    Balance REAL NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Scooters (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    LockCode TEXT NOT NULL,
                    IsLocked INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS RentalSessions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    ScooterId INTEGER NOT NULL,
                    StartTime TEXT NOT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 1
                );
            ", connection);
            createTablesCmd.ExecuteNonQuery();

            // Добавляем тестовые данные
            SqliteCommand checkUsersCmd = new SqliteCommand("SELECT COUNT(*) FROM Users", connection);
            long userCount = (long)checkUsersCmd.ExecuteScalar();

            if (userCount == 0)
            {
                SqliteCommand insertUserCmd = new SqliteCommand(
                    "INSERT INTO Users (Name, Phone, Balance) VALUES ('Иван Петров', '+79991234567', 500)",
                    connection);
                insertUserCmd.ExecuteNonQuery();
            }

            SqliteCommand checkScootersCmd = new SqliteCommand("SELECT COUNT(*) FROM Scooters", connection);
            long scooterCount = (long)checkScootersCmd.ExecuteScalar();

            if (scooterCount == 0)
            {
                SqliteCommand insertScooter1Cmd = new SqliteCommand(
                    "INSERT INTO Scooters (LockCode, IsLocked) VALUES ('1234', 1)",
                    connection);
                insertScooter1Cmd.ExecuteNonQuery();

                SqliteCommand insertScooter2Cmd = new SqliteCommand(
                    "INSERT INTO Scooters (LockCode, IsLocked) VALUES ('5678', 1)",
                    connection);
                insertScooter2Cmd.ExecuteNonQuery();
            }

            // Получаем список доступных самокатов
            SqliteCommand getScootersCmd = new SqliteCommand(
                "SELECT Id, LockCode, IsLocked FROM Scooters WHERE IsLocked = 1", connection);

            SqliteDataReader reader = getScootersCmd.ExecuteReader();

            // Создаем HTML прямо в контроллере
            string html = "<!DOCTYPE html><html><head><title>Аренда самокатов</title></head><body>";
            html += "<h1>Аренда самокатов</h1>";

            // Информация о пользователе
            SqliteCommand getUserCmd = new SqliteCommand(
                "SELECT Id, Name, Balance FROM Users LIMIT 1", connection);
            SqliteDataReader userReader = getUserCmd.ExecuteReader();
            int currentUserId = 1;
            string userName = "Неизвестно";
            decimal userBalance = 0;

            if (userReader.Read())
            {
                currentUserId = userReader.GetInt32(0);
                userName = userReader.GetString(1);
                userBalance = (decimal)userReader.GetDouble(2);
            }
            userReader.Close();

            html += $"<p>Пользователь: {userName}, Баланс: {userBalance} руб.</p>";
            html += "<table border='1'><tr><th>ID</th><th>Статус</th><th>Действие</th></tr>";

            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                bool isLocked = reader.GetInt32(2) == 1;
                string status = isLocked ? "Доступен" : "Занят";

                html += $"<tr><td>{id}</td><td>{status}</td>";
                if (isLocked)
                {
                    html += $"<td><form method='post' action='/Rental/StartRental'>";
                    html += $"<input type='hidden' name='userId' value='{currentUserId}'/>";
                    html += $"<input type='hidden' name='scooterId' value='{id}'/>";
                    html += $"<button type='submit'>Арендовать</button></form></td>";
                }
                else
                {
                    html += "<td>Недоступен</td>";
                }
                html += "</tr>";
            }
            reader.Close();
            html += "</table>";
            html += "</body></html>";

            return Content(html, "text/html", System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            return Content($"Ошибка: {ex.Message}");
        }
        finally
        {
            connection.Close();
        }
    }

    // POST: Начать аренду
    [HttpPost]
    public IActionResult StartRental(int userId, int scooterId)
    {
        SqliteConnection connection = new SqliteConnection("Data Source=scooters.db");

        try
        {
            connection.Open();

            // Проверяем баланс пользователя
            SqliteCommand checkBalanceCmd = new SqliteCommand(
                "SELECT Balance FROM Users WHERE Id = @userId", connection);
            checkBalanceCmd.Parameters.AddWithValue("@userId", userId);

            var balanceResult = checkBalanceCmd.ExecuteScalar();
            if (balanceResult == null)
            {
                return BadRequest("Пользователь не найден");
            }

            decimal balance = (decimal)(double)balanceResult;

            // Проверяем, достаточно ли средств
            if (balance < 50)
            {
                return BadRequest("Недостаточно средств на балансе");
            }

            // Получаем информацию о самокате
            SqliteCommand getScooterCmd = new SqliteCommand(
                "SELECT LockCode, IsLocked FROM Scooters WHERE Id = @scooterId", connection);
            getScooterCmd.Parameters.AddWithValue("@scooterId", scooterId);

            SqliteDataReader reader = getScooterCmd.ExecuteReader();
            if (!reader.Read())
            {
                reader.Close();
                return BadRequest("Самокат не найден");
            }

            string lockCode = reader.GetString(0);
            bool isLocked = reader.GetInt32(1) == 1;
            reader.Close();

            // Проверяем, не арендован ли уже самокат
            if (!isLocked)
            {
                return BadRequest("Самокат уже используется");
            }

            // Имитация разблокировки замка
            System.Threading.Thread.Sleep(500);

            // Создаем сессию аренды
            SqliteCommand createSessionCmd = new SqliteCommand(
                @"INSERT INTO RentalSessions (UserId, ScooterId, StartTime, IsActive) 
                  VALUES (@uid, @sid, @start, 1);
                  SELECT last_insert_rowid();", connection);
            createSessionCmd.Parameters.AddWithValue("@uid", userId);
            createSessionCmd.Parameters.AddWithValue("@sid", scooterId);
            createSessionCmd.Parameters.AddWithValue("@start", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            long sessionId = (long)createSessionCmd.ExecuteScalar();

            // Обновляем статус самоката
            SqliteCommand updateScooterCmd = new SqliteCommand(
                "UPDATE Scooters SET IsLocked = 0 WHERE Id = @scooterId", connection);
            updateScooterCmd.Parameters.AddWithValue("@scooterId", scooterId);
            updateScooterCmd.ExecuteNonQuery();

            // Имитация SMS-уведомления
            string phone = GetUserPhone(userId);
            Console.WriteLine($"SMS отправлено на {phone}: Самокат разблокирован");

            // Логирование в файл
            System.IO.File.AppendAllText("rental_log.txt",
                $"{DateTime.Now}: Пользователь {userId} арендовал самокат {scooterId}\n");

            return RedirectToAction("Success", new { sessionId });
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText("error_log.txt",
                $"{DateTime.Now}: Ошибка: {ex.Message}\n");
            return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
        }
        finally
        {
            connection.Close();
        }
    }

    // GET: Страница успешной аренды
    [HttpGet]
    public IActionResult Success(int sessionId)
    {
        SqliteConnection connection = new SqliteConnection("Data Source=scooters.db");

        try
        {
            connection.Open();

            SqliteCommand cmd = new SqliteCommand(@"
                SELECT 
                    u.Name, 
                    s.LockCode, 
                    rs.StartTime
                FROM RentalSessions rs
                JOIN Users u ON rs.UserId = u.Id
                JOIN Scooters s ON rs.ScooterId = s.Id
                WHERE rs.Id = @sessionId", connection);
            cmd.Parameters.AddWithValue("@sessionId", sessionId);

            SqliteDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string userName = reader.GetString(0);
                string lockCode = reader.GetString(1);
                string startTimeStr = reader.GetString(2);

                string html = "<!DOCTYPE html><html><head><title>Аренда начата</title></head><body>";
                html += "<h1>Аренда успешно начата!</h1>";
                html += $"<p>Пользователь: {userName}</p>";
                html += $"<p>Код замка: {lockCode}</p>";
                html += $"<p>Время начала: {startTimeStr}</p>";
                html += "<p>Самокат разблокирован.</p>";
                html += "<a href='/Rental/Index'>Вернуться к списку</a>";
                html += "</body></html>";

                return Content(html, "text/html", System.Text.Encoding.UTF8);
            }

            return Content("Сессия не найдена");
        }
        catch (Exception ex)
        {
            return Content($"Ошибка: {ex.Message}");
        }
        finally
        {
            connection.Close();
        }
    }

    // Вспомогательный метод
    private string GetUserPhone(int userId)
    {
        using (SqliteConnection conn = new SqliteConnection("Data Source=scooters.db"))
        {
            conn.Open();
            SqliteCommand cmd = new SqliteCommand("SELECT Phone FROM Users WHERE Id = @id", conn);
            cmd.Parameters.AddWithValue("@id", userId);
            var result = cmd.ExecuteScalar();
            return result?.ToString() ?? "";
        }
    }
}