using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using shetuan.Models;
using System.Net.Mail;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var app = builder.Build();
app.UseStaticFiles();
app.MapControllers();

// 数据文件路径
const string userDataFile = "alldata/user.json";
const string activityDataFile = "alldata/activity.json";
const string registrationDataFile = "alldata/registration.json";
const string notificationDataFile = "alldata/notification.json";

// 加载用户数据
List<User> LoadUsers()
{
    if (!File.Exists(userDataFile)) return new List<User>();
    var json = File.ReadAllText(userDataFile);
    return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
}
List<Admin> LoadAdmins()
{
    const string adminDataFile = "alldata/admin.json";
    if (!File.Exists(adminDataFile)) return new List<Admin>();
    var json = File.ReadAllText(adminDataFile);
    return JsonSerializer.Deserialize<List<Admin>>(json) ?? new List<Admin>();
}
void SaveUsers(List<User> users)
{
    var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(userDataFile, json);
}
// 加载活动数据
List<Activity> LoadActivities()
{
    if (!File.Exists(activityDataFile)) return new List<Activity>();
    var json = File.ReadAllText(activityDataFile);
    return JsonSerializer.Deserialize<List<Activity>>(json) ?? new List<Activity>();
}
// 加载通知数据
List<Notification> LoadNotifications()
{
    if (!File.Exists(notificationDataFile)) return new List<Notification>();
    var json = File.ReadAllText(notificationDataFile);
    return JsonSerializer.Deserialize<List<Notification>>(json) ?? new List<Notification>();
}
void SaveNotifications(List<Notification> notifications)
{
    var json = JsonSerializer.Serialize(notifications, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(notificationDataFile, json);
}
// 加载报名数据
List<Registration> LoadRegistrations()
{
    if (!File.Exists(registrationDataFile)) return new List<Registration>();
    var json = File.ReadAllText(registrationDataFile);
    return JsonSerializer.Deserialize<List<Registration>>(json) ?? new List<Registration>();
}

// 登录接口：同步返回邮箱
app.MapGet("/login", (string username, string password) =>
{
    var users = LoadUsers();
    var user = users.FirstOrDefault(u =>
        string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) &&
        u.Password == password);

    if (user == null)
        return Results.Json(new { success = false, message = "登录失败：用户名或密码错误！" });

    return Results.Json(new
    {
        success = true,
        message = $"登录成功！欢迎，{username}！",
        data = new
        {
            username = user.Username,
            name = user.Name,
            email = user.Email,
            role = user.Role
        }
    });
});

// 管理员登录接口
app.MapGet("/admin_login", (string username, string password) =>
{
    var admins = LoadAdmins(); 
    var admin = admins.FirstOrDefault(a =>
        string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase) &&
        a.Password == password);

    if (admin == null)
        return Results.Json(new { success = false, message = "管理员用户名或密码错误！" });

    return Results.Json(new
    {
        success = true,
        message = $"管理员登录成功，欢迎 {admin.Name}！",
        data = new
        {
            username = admin.Username,
            name = admin.Name,
            email = admin.Email,
            role = admin.Role
        }
    });
});

// 注册接口
app.MapPost("/register", async (HttpContext context) =>
{
    var users = LoadUsers();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });

    var doc = JsonDocument.Parse(body);

    if (!doc.RootElement.TryGetProperty("username", out var usernameElem) ||
        !doc.RootElement.TryGetProperty("password", out var passwordElem) ||
        !doc.RootElement.TryGetProperty("name", out var nameElem) ||
        !doc.RootElement.TryGetProperty("email", out var emailElem))
        return Results.Json(new { success = false, message = "缺少参数！" });

    string username = usernameElem.GetString();
    string password = passwordElem.GetString();
    string name = nameElem.GetString();
    string email = emailElem.GetString();

    if (users.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)))
        return Results.Json(new { success = false, message = "用户名已存在！" });
    if (users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)))
        return Results.Json(new { success = false, message = "该邮箱已注册！" });

    var user = new User
    {
        Username = username,
        Password = password,
        Name = name,
        Email = email,
        Role = "user",
        CreatedAt = DateTime.Now
    };
    users.Add(user);
    SaveUsers(users);

    return Results.Json(new { success = true, message = "注册成功！请登录。" });
});

// 个人资料查询接口
app.MapGet("/my_profile", (string username) =>
{
    var users = LoadUsers();
    var user = users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
    if (user == null)
        return Results.Json(new { success = false, message = "未找到该用户！" });

    return Results.Json(new
    {
        success = true,
        data = new
        {
            username = user.Username,
            name = user.Name,
            email = user.Email,
            role = user.Role,
            createdAt = user.CreatedAt
        }
    });
});

// 个人资料更新接口
app.MapPost("/update_profile", async (HttpContext context) =>
{
    var users = LoadUsers();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });

    var doc = JsonDocument.Parse(body);
    if (!doc.RootElement.TryGetProperty("Username", out var unameElement))
        return Results.Json(new { success = false, message = "缺少用户名！" });

    var username = unameElement.GetString();
    var user = users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
    if (user == null)
        return Results.Json(new { success = false, message = "未找到该用户！" });

    bool updated = false;
    if (doc.RootElement.TryGetProperty("Name", out var nameElement))
    {
        user.Name = nameElement.GetString();
        updated = true;
    }
    if (doc.RootElement.TryGetProperty("Email", out var emailElement))
    {
        user.Email = emailElement.GetString();
        updated = true;
    }
    if (doc.RootElement.TryGetProperty("Password", out var pwdElement))
    {
        user.Password = pwdElement.GetString();
        updated = true;
    }

    if (updated)
    {
        SaveUsers(users);
        return Results.Json(new { success = true, message = "修改成功！" });
    }
    else
    {
        return Results.Json(new { success = false, message = "无可更新项！" });
    }
});

// 活动相关接口
app.MapGet("/activities", () =>
{
    var activities = LoadActivities();
    var list = activities.Select(a => new
    {
        a.Id,
        Name = a.Title,
        a.EventTime,
        a.Location,
        a.Description,
        a.Status,
        a.CreatorEmail
    }).ToList();
    return Results.Json(new { success = true, data = list });
});


// 报名相关接口
app.MapPost("/register_activity", async (HttpContext context) =>
{
    var regs = LoadRegistrations();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });

    var doc = JsonDocument.Parse(body);

    if (!doc.RootElement.TryGetProperty("ActivityId", out var actIdElem) ||
        !doc.RootElement.TryGetProperty("UserEmail", out var emailElem) ||
        !doc.RootElement.TryGetProperty("ActivityName", out var actNameElem))
        return Results.Json(new { success = false, message = "缺少参数！" });

    int activityId = actIdElem.GetInt32();
    string userEmail = emailElem.GetString();
    string activityName = actNameElem.GetString();

    if (regs.Any(r => r.ActivityId == activityId && string.Equals(r.UserEmail, userEmail, StringComparison.OrdinalIgnoreCase)))
        return Results.Json(new { success = false, message = "您已报名该活动，请勿重复报名！" });

    var reg = new Registration
    {
        Id = regs.Count > 0 ? regs.Max(r => r.Id) + 1 : 1,
        ActivityId = activityId,
        ActivityName = activityName,
        UserEmail = userEmail,
        Status = "pending",
        AppliedAt = DateTime.Now
    };
    regs.Add(reg);
    var json = JsonSerializer.Serialize(regs, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(registrationDataFile, json);

    return Results.Json(new { success = true, message = "报名已提交，等待管理员审核。" });
});

app.MapGet("/my_registrations", (string email) =>
{
    var regs = LoadRegistrations();
    var userRegs = regs.Where(r => string.Equals(r.UserEmail, email, StringComparison.OrdinalIgnoreCase)).ToList();
    return Results.Json(new { success = true, data = userRegs });
});

app.MapPost("/cancel_registration", async (HttpContext context) =>
{
    var regs = LoadRegistrations();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });

    var doc = JsonDocument.Parse(body);
    if (!doc.RootElement.TryGetProperty("ActivityId", out var actIdElem) ||
        !doc.RootElement.TryGetProperty("UserEmail", out var emailElem))
        return Results.Json(new { success = false, message = "缺少参数！" });

    int activityId = actIdElem.GetInt32();
    string userEmail = emailElem.GetString();

    var reg = regs.FirstOrDefault(r => r.ActivityId == activityId && string.Equals(r.UserEmail, userEmail, StringComparison.OrdinalIgnoreCase));
    if (reg == null)
        return Results.Json(new { success = false, message = "未找到该报名记录！" });

    regs.Remove(reg);
    var json = JsonSerializer.Serialize(regs, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(registrationDataFile, json);

    return Results.Json(new { success = true, message = "报名已取消！" });
});

// 通知相关接口
app.MapGet("/notifications", (string email) =>
{
    var notifications = LoadNotifications()
        .Where(n => string.Equals(n.RecipientEmail, email, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(n => n.CreatedAt)
        .ToList();
    return Results.Json(new { success = true, data = notifications });
});

app.MapPost("/mark_notification_read", async (HttpContext context) =>
{
    var notifications = LoadNotifications();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });
    var doc = JsonDocument.Parse(body);
    var root = doc.RootElement;
    if (!root.TryGetProperty("Id", out var idElem) ||
        !root.TryGetProperty("RecipientEmail", out var emailElem))
        return Results.Json(new { success = false, message = "缺少参数！" });
    int id = idElem.GetInt32();
    string email = emailElem.GetString();
    var noti = notifications.FirstOrDefault(n => n.Id == id && n.RecipientEmail.Equals(email, StringComparison.OrdinalIgnoreCase));
    if (noti == null)
        return Results.Json(new { success = false, message = "未找到通知！" });
    noti.IsRead = true;
    SaveNotifications(notifications);
    return Results.Json(new { success = true });
});

app.MapPost("/mark_all_notifications_read", async (HttpContext context) =>
{
    var notifications = LoadNotifications();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });
    var doc = JsonDocument.Parse(body);
    var root = doc.RootElement;
    if (!root.TryGetProperty("RecipientEmail", out var emailElem))
        return Results.Json(new { success = false, message = "缺少邮箱参数！" });
    string email = emailElem.GetString();
    int count = 0;
    foreach (var noti in notifications.Where(n => n.RecipientEmail.Equals(email, StringComparison.OrdinalIgnoreCase) && !n.IsRead))
    {
        noti.IsRead = true;
        count++;
    }
    SaveNotifications(notifications);
    return Results.Json(new { success = true, message = $"已标记{count}条为已读" });
});

// 发送邮箱验证码相关
Dictionary<string, (string code, DateTime expire)> emailCodes = new();
void SendEmail(string to, string subject, string body)
{
    var smtpHost = "smtp.qq.com"; 
    var smtpPort = 587;           
    var smtpUser = "2252159453@qq.com"; 
    var smtpPass = "rkqlxrxiovsleabe";  

    var msg = new MailMessage();
    msg.From = new MailAddress(smtpUser, "吉林大学社团管理系统");
    msg.To.Add(to);
    msg.Subject = subject;
    msg.Body = body;

    using var client = new SmtpClient(smtpHost, smtpPort)
    {
        EnableSsl = true,
        Credentials = new NetworkCredential(smtpUser, smtpPass)
    };
    client.Send(msg);
}
app.MapPost("/send_login_code", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    var doc = JsonDocument.Parse(body);
    if (!doc.RootElement.TryGetProperty("email", out var emailElem))
        return Results.Json(new { success = false, message = "缺少邮箱！" });
    string email = emailElem.GetString();

    var users = LoadUsers();
    var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    if (user == null)
        return Results.Json(new { success = false, message = "该邮箱未注册！" });

    var rnd = new Random();
    string code = rnd.Next(100000, 999999).ToString();
    emailCodes[email.ToLower()] = (code, DateTime.Now.AddMinutes(5));

    try
    {
        SendEmail(email, "您的登录验证码", $"您的验证码是：{code}，5分钟内有效。");
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, message = "邮件发送失败：" + ex.Message });
    }

    return Results.Json(new { success = true, message = "验证码已发送到邮箱，请查收。" });
});
app.MapPost("/verify_login_code", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    var doc = JsonDocument.Parse(body);

    if (!doc.RootElement.TryGetProperty("email", out var emailElem) ||
        !doc.RootElement.TryGetProperty("code", out var codeElem))
        return Results.Json(new { success = false, message = "缺少参数！" });

    string email = emailElem.GetString();
    string code = codeElem.GetString();
    var key = email.ToLower();

    if (!emailCodes.ContainsKey(key) || emailCodes[key].expire < DateTime.Now)
        return Results.Json(new { success = false, message = "验证码不存在或已过期，请重新获取！" });
    if (emailCodes[key].code != code)
        return Results.Json(new { success = false, message = "验证码错误，请重试！" });

    emailCodes.Remove(key);

    return Results.Json(new { success = true, message = "登录成功！" });
});
// 管理员获取所有报名申请
app.MapGet("/all_registrations", () =>
{
    var regs = LoadRegistrations();
    return Results.Json(new { success = true, data = regs });
});
app.MapGet("/user_by_email", (string email) =>
{
    var users = LoadUsers();
    var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    if (user != null)
        return Results.Json(new
        {
            success = true,
            data = new
            {
                username = user.Username,
                name = user.Name,
                email = user.Email,
                role = user.Role,
                createdAt = user.CreatedAt
            }
        });
    var admins = LoadAdmins();
    var admin = admins.FirstOrDefault(a => a.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    if (admin != null)
        return Results.Json(new
        {
            success = true,
            data = new
            {
                username = admin.Username,
                name = admin.Name,
                email = admin.Email,
                role = admin.Role,
                createdAt = admin.CreatedAt
            }
        });
    return Results.Json(new { success = false, message = "未找到该用户！" });
});
app.MapPost("/edit_activity", async (HttpContext context) =>
{
    var activities = LoadActivities();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });

    var doc = JsonDocument.Parse(body);
    if (!doc.RootElement.TryGetProperty("Id", out var idElem))
        return Results.Json(new { success = false, message = "缺少活动ID！" });
    int id = idElem.GetInt32();
    var activity = activities.FirstOrDefault(a => a.Id == id);
    if (activity == null)
        return Results.Json(new { success = false, message = "活动不存在！" });

    // 更新字段
    if (doc.RootElement.TryGetProperty("Name", out var nameElem))
        activity.Title = nameElem.GetString();
    if (doc.RootElement.TryGetProperty("EventTime", out var timeElem))
        activity.EventTime = DateTime.Parse(timeElem.GetString());
    if (doc.RootElement.TryGetProperty("Location", out var locElem))
        activity.Location = locElem.GetString();
    if (doc.RootElement.TryGetProperty("Description", out var descElem))
        activity.Description = descElem.GetString();
    if (doc.RootElement.TryGetProperty("Status", out var statusElem))
        activity.Status = statusElem.GetString();

    // 保存
    var json = JsonSerializer.Serialize(activities, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(activityDataFile, json);

    return Results.Json(new { success = true, message = "活动信息已更新！" });
});
app.MapPost("/add_activity", async (HttpContext context) =>
{
    var activities = LoadActivities();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });
    var doc = JsonDocument.Parse(body);

    string name = doc.RootElement.GetProperty("Name").GetString();
    DateTime eventTime = DateTime.Parse(doc.RootElement.GetProperty("EventTime").GetString());
    string location = doc.RootElement.GetProperty("Location").GetString();
    string description = doc.RootElement.GetProperty("Description").GetString();
    string status = doc.RootElement.GetProperty("Status").GetString();

    var newActivity = new Activity
    {
        Id = activities.Count > 0 ? activities.Max(a => a.Id) + 1 : 1,
        Title = name,
        EventTime = eventTime,
        Location = location,
        Description = description,
        Status = status,
        CreatorEmail = "admin" // 或当前管理员邮箱
    };
    activities.Add(newActivity);
    var json = JsonSerializer.Serialize(activities, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(activityDataFile, json);

    return Results.Json(new { success = true, message = "活动已创建！" });
});
app.MapPost("/delete_activity", async (HttpContext context) =>
{
    var activities = LoadActivities();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });

    var doc = JsonDocument.Parse(body);
    if (!doc.RootElement.TryGetProperty("Id", out var idElem))
        return Results.Json(new { success = false, message = "缺少活动ID！" });

    int id = idElem.GetInt32();
    var act = activities.FirstOrDefault(a => a.Id == id);
    if (act == null)
        return Results.Json(new { success = false, message = "未找到该活动！" });

    activities.Remove(act);

    var json = JsonSerializer.Serialize(activities, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(activityDataFile, json);

    return Results.Json(new { success = true, message = "活动已删除！" });
});
// 审核通过报名申请
app.MapPost("/approve_registration", async (HttpContext context) =>
{
    var regs = LoadRegistrations();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });

    var doc = JsonDocument.Parse(body);
    if (!doc.RootElement.TryGetProperty("Id", out var idElem))
        return Results.Json(new { success = false, message = "缺少报名ID！" });
    int id = idElem.GetInt32();
    var reg = regs.FirstOrDefault(r => r.Id == id);
    if (reg == null)
        return Results.Json(new { success = false, message = "未找到报名记录！" });

    reg.Status = "approved";
    var json = JsonSerializer.Serialize(regs, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(registrationDataFile, json);

    return Results.Json(new { success = true, message = "已通过该报名申请！" });
});

// 审核拒绝报名申请
app.MapPost("/reject_registration", async (HttpContext context) =>
{
    var regs = LoadRegistrations();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });

    var doc = JsonDocument.Parse(body);
    if (!doc.RootElement.TryGetProperty("Id", out var idElem))
        return Results.Json(new { success = false, message = "缺少报名ID！" });
    int id = idElem.GetInt32();
    var reg = regs.FirstOrDefault(r => r.Id == id);
    if (reg == null)
        return Results.Json(new { success = false, message = "未找到报名记录！" });

    reg.Status = "rejected";
    var json = JsonSerializer.Serialize(regs, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(registrationDataFile, json);

    return Results.Json(new { success = true, message = "已拒绝该报名申请！" });
});
app.MapPost("/review_registration", async (HttpContext context) =>
{
    var regs = LoadRegistrations();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });

    var doc = JsonDocument.Parse(body);

    if (!doc.RootElement.TryGetProperty("Id", out var idElem) ||
        !doc.RootElement.TryGetProperty("Status", out var statusElem))
        return Results.Json(new { success = false, message = "缺少参数！" });

    int id = idElem.GetInt32();
    string status = statusElem.GetString();

    var reg = regs.FirstOrDefault(r => r.Id == id);
    if (reg == null)
        return Results.Json(new { success = false, message = "未找到报名记录！" });

    if (status != "approved" && status != "rejected")
        return Results.Json(new { success = false, message = "未知的审核操作！" });

    reg.Status = status;

    var json = JsonSerializer.Serialize(regs, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText("alldata/registration.json", json);

    return Results.Json(new { success = true, message = "操作成功！" });
});
// 假设用内存存token（生产建议redis等）
Dictionary<string, (string email, DateTime expire)> loginTokens = new();

app.MapPost("/send_login_link", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    var doc = JsonDocument.Parse(body);
    if (!doc.RootElement.TryGetProperty("email", out var emailElem))
        return Results.Json(new { success = false, message = "缺少邮箱！" });
    string email = emailElem.GetString();

    var users = LoadUsers();
    var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    if (user == null)
        return Results.Json(new { success = false, message = "该邮箱未注册！" });

    // 生成token并保存
    string token = Guid.NewGuid().ToString("N");
    loginTokens[token] = (email, DateTime.Now.AddMinutes(10)); // 10分钟有效

    // 构造URL
    var host = context.Request.Host.Value;
    var scheme = context.Request.Scheme;
    string url = $"{scheme}://{host}/email_login.html?token={token}";

    // 发送邮件
    try
    {
        SendEmail(email, "您的登录链接", $"请点击下方链接登录（10分钟内有效）：\n{url}");
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, message = "邮件发送失败：" + ex.Message });
    }

    return Results.Json(new { success = true, message = "登录链接已发送至邮箱，请查收。" });
});
app.MapPost("/verify_token_login", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    var doc = JsonDocument.Parse(body);
    if (!doc.RootElement.TryGetProperty("token", out var tokenElem))
        return Results.Json(new { success = false, message = "缺少token！" });
    string token = tokenElem.GetString();

    if (!loginTokens.ContainsKey(token) || loginTokens[token].expire < DateTime.Now)
        return Results.Json(new { success = false, message = "token无效或已过期！" });

    var email = loginTokens[token].email;
    loginTokens.Remove(token);

    // 查用户信息
    var users = LoadUsers();
    var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    if (user == null)
        return Results.Json(new { success = false, message = "未找到该用户！" });

    return Results.Json(new
    {
        success = true,
        data = new
        {
            username = user.Username,
            name = user.Name,
            email = user.Email,
            role = user.Role
        }
    });
});
app.MapFallbackToFile("index.html");
app.Run();

