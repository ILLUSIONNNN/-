using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using shetuan.Models;

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
            email = user.Email
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

// 登录接口
app.MapGet("/login", (string username, string password) =>
{
    var users = LoadUsers();
    var user = users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) && u.Password == password);

    if (user == null)
        return Results.Json(new { success = false, message = "登录失败：用户名或密码错误！" });

    return Results.Json(new { success = true, message = $"登录成功！欢迎，{username}！" });
});

// 查询全部活动（访客和用户都能看）


// 查询用户通知接口
app.MapGet("/notifications", (string email) =>
{
    var notifications = LoadNotifications()
        .Where(n => string.Equals(n.RecipientEmail, email, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(n => n.CreatedAt)
        .ToList();
    return Results.Json(new { success = true, data = notifications });
});

// 查询我的报名（根据邮箱）
app.MapGet("/my_registrations", (string email) =>
{
    var regs = LoadRegistrations();
    // 只筛选该用户的报名数据
    var userRegs = regs.Where(r => string.Equals(r.UserEmail, email, StringComparison.OrdinalIgnoreCase)).ToList();
    return Results.Json(new { success = true, data = userRegs });
});

// 取消报名接口
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


app.MapPost("/register", async (HttpContext context) =>
{
    var users = LoadUsers();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });

    var doc = JsonDocument.Parse(body);

    // 检查字段
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
        Email = email
    };
    users.Add(user);
    SaveUsers(users);

    return Results.Json(new { success = true, message = "注册成功！请登录。" });
});
app.MapGet("/admin_login", (string username, string password) =>
{
    var admins = LoadAdmins(); // 这里LoadAdmins()要从admin.json读取管理员
    var admin = admins.FirstOrDefault(a => a.Username == username && a.Password == password);

    if (admin == null)
        return Results.Json(new { success = false, message = "管理员用户名或密码错误！" });

    return Results.Json(new { success = true, message = $"管理员登录成功，欢迎 {admin.Name}！" });
});



// 保存活动
void SaveActivities(List<Activity> activities)
{
    var json = JsonSerializer.Serialize(activities, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(activityDataFile, json);
}

app.MapGet("/activities", () =>
{
    var activities = LoadActivities();
    // 为了兼容前端，返回 Name 字段
    var list = activities.Select(a => new {
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
// 2. 新建活动
app.MapPost("/add_activity", async (HttpContext context) =>
{
    var activities = LoadActivities();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });

    var doc = JsonDocument.Parse(body);
    var root = doc.RootElement;

    // 这里前端字段是 Name，模型字段是 Title
    if (!root.TryGetProperty("Name", out var nameElem) ||
        !root.TryGetProperty("EventTime", out var timeElem) ||
        !root.TryGetProperty("Location", out var locElem))
        return Results.Json(new { success = false, message = "缺少必填项！" });

    var activity = new Activity
    {
        Id = activities.Count > 0 ? activities.Max(a => a.Id) + 1 : 1,
        Title = nameElem.GetString(), // ← 这里赋值给 Title
        EventTime = DateTime.Parse(timeElem.GetString()),
        Location = locElem.GetString(),
        Description = root.TryGetProperty("Description", out var descElem) ? descElem.GetString() : "",
        Status = root.TryGetProperty("Status", out var statusElem) ? statusElem.GetString() : "未开始",
        CreatorEmail = "" // 你可以根据需求设置
    };

    activities.Add(activity);
    SaveActivities(activities);

    return Results.Json(new { success = true, message = "活动创建成功！" });
});

// 3. 编辑活动
app.MapPost("/edit_activity", async (HttpContext context) =>
{
    var activities = LoadActivities();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });

    var doc = JsonDocument.Parse(body);
    var root = doc.RootElement;

    if (!root.TryGetProperty("Id", out var idElem))
        return Results.Json(new { success = false, message = "缺少活动Id！" });

    int id = idElem.GetInt32();
    var activity = activities.FirstOrDefault(a => a.Id == id);
    if (activity == null)
        return Results.Json(new { success = false, message = "未找到该活动！" });

    if (root.TryGetProperty("Name", out var nameElem))
        activity.Title = nameElem.GetString(); // ← 这里赋值给 Title
    if (root.TryGetProperty("EventTime", out var timeElem))
        activity.EventTime = DateTime.Parse(timeElem.GetString());
    if (root.TryGetProperty("Location", out var locElem))
        activity.Location = locElem.GetString();
    if (root.TryGetProperty("Description", out var descElem))
        activity.Description = descElem.GetString();
    if (root.TryGetProperty("Status", out var statusElem))
        activity.Status = statusElem.GetString();

    SaveActivities(activities);

    return Results.Json(new { success = true, message = "活动修改成功！" });
});

// 4. 删除活动
app.MapPost("/delete_activity", async (HttpContext context) =>
{
    var activities = LoadActivities();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });

    var doc = JsonDocument.Parse(body);
    var root = doc.RootElement;
    if (!root.TryGetProperty("Id", out var idElem))
        return Results.Json(new { success = false, message = "缺少活动Id！" });

    int id = idElem.GetInt32();
    var activity = activities.FirstOrDefault(a => a.Id == id);
    if (activity == null)
        return Results.Json(new { success = false, message = "未找到该活动！" });

    activities.Remove(activity);
    SaveActivities(activities);

    return Results.Json(new { success = true, message = "活动已删除！" });
});
app.MapPost("/review_registration", async (HttpContext context) =>
{
    var regs = LoadRegistrations();
    var notifications = LoadNotifications();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });

    var doc = JsonDocument.Parse(body);
    var root = doc.RootElement;
    if (!root.TryGetProperty("Id", out var idElem) ||
        !root.TryGetProperty("Status", out var statusElem))
        return Results.Json(new { success = false, message = "缺少参数！" });

    int id = idElem.GetInt32();
    string status = statusElem.GetString();
    var reg = regs.FirstOrDefault(r => r.Id == id);
    if (reg == null)
        return Results.Json(new { success = false, message = "未找到该报名申请！" });

    reg.Status = status;

    // 发送通知
    string notifyTitle, notifyContent;
    if (status == "approved")
    {
        notifyTitle = "活动报名审核通过";
        notifyContent = $"您报名的活动【{reg.ActivityName}】已通过审核，欢迎参加！";
    }
    else
    {
        notifyTitle = "活动报名被拒绝";
        notifyContent = $"很抱歉，您报名的活动【{reg.ActivityName}】未通过审核。";
    }
    notifications.Add(new Notification
    {
        Id = notifications.Count > 0 ? notifications.Max(n => n.Id) + 1 : 1,
        RecipientEmail = reg.UserEmail,
 
        Content = notifyContent,
        CreatedAt = DateTime.Now
    });
    SaveNotifications(notifications);

    var json = JsonSerializer.Serialize(regs, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(registrationDataFile, json);

    return Results.Json(new { success = true, message = status == "approved" ? "审核通过！" : "已拒绝报名。" });
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

// 标记所有通知为已读
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
app.MapPost("/register_activity", async (HttpContext context) =>
{
    var regs = LoadRegistrations();
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body))
        return Results.Json(new { success = false, message = "请求体为空！" });

    var doc = JsonDocument.Parse(body);

    // 检查参数
    if (!doc.RootElement.TryGetProperty("ActivityId", out var actIdElem) ||
        !doc.RootElement.TryGetProperty("UserEmail", out var emailElem) ||
        !doc.RootElement.TryGetProperty("ActivityName", out var actNameElem))
        return Results.Json(new { success = false, message = "缺少参数！" });

    int activityId = actIdElem.GetInt32();
    string userEmail = emailElem.GetString();
    string activityName = actNameElem.GetString();

    // 防止重复报名
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

    // 返回“已提交，等待审核”
    return Results.Json(new { success = true, message = "报名已提交，等待管理员审核。" });
});
// 管理员获取所有报名申请（用于审核页面）
app.MapGet("/all_registrations", () =>
{
    var regs = LoadRegistrations();
    return Results.Json(new { success = true, data = regs });
});
app.MapFallbackToFile("index.html");
app.Run();// 添加 LoadAdmins 方法以解决 CS0103 错误
List<Admin> LoadAdmins()
{
    const string adminDataFile = "alldata/admin.json";
    if (!File.Exists(adminDataFile)) return new List<Admin>();
    var json = File.ReadAllText(adminDataFile);
    return JsonSerializer.Deserialize<List<Admin>>(json) ?? new List<Admin>();
}
// 标记单条通知为已读
