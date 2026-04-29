# دليل الأمان - SASMS

## الميزات الأمنية المطبقة

### 1. تشفير كلمة المرور
- استخدام BCrypt لتشفير كلمات المرور
- Salt تلقائي مع كل كلمة مرور
- Cost factor = 12 (مستوى أمان عالي)

### 2. Authentication (المصادقة)
- Cookie-based Authentication
- Session timeout: 30 يوم
- Sliding expiration مفعل
- HttpOnly cookies لمنع XSS
- Secure cookies في بيئة الإنتاج

### 3. Authorization (الصلاحيات)
- Role-based Authorization
- Policies محددة:
  - `AdminOnly`: للإدارة فقط
  - `StudentAffairsOnly`: لشؤون الطلاب والإدارة
  - `StudentOnly`: للطلاب فقط
  - `ApplicantOnly`: للمتقدمين فقط
  - `Authenticated`: لأي مستخدم مسجل دخول

### 4. Security Headers
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- X-XSS-Protection: 1; mode=block
- Referrer-Policy: strict-origin-when-cross-origin
- Permissions-Policy

### 5. Anti-Forgery Token
- CSRF protection في جميع النماذج
- ValidateAntiForgeryToken attribute

### 6. Input Validation
- Server-side validation
- Client-side validation
- SQL Injection protection (Entity Framework parameterized queries)

### 7. Logging
- تسجيل محاولات تسجيل الدخول الفاشلة
- تسجيل إنشاء الحسابات الجديدة
- تسجيل تسجيل الخروج

## كيفية الاستخدام

### حماية Controller/Action
```csharp
[Authorize] // يتطلب تسجيل دخول
public IActionResult ProtectedAction()
{
    return View();
}

[Authorize(Policy = "AdminOnly")] // للإدارة فقط
public IActionResult AdminAction()
{
    return View();
}

[AuthorizeByUserType("Student", "StudentAffairs")] // أنواع محددة
public IActionResult StudentAction()
{
    return View();
}
```

### الحصول على المستخدم الحالي
```csharp
var user = _authService.GetCurrentUser(HttpContext);
```

## ملاحظات مهمة

1. **قاعدة البيانات**: تأكد من تشغيل migrations لإنشاء قاعدة البيانات
2. **Connection String**: قم بتحديث connection string في appsettings.json
3. **HTTPS**: في الإنتاج، تأكد من تفعيل HTTPS
4. **Secrets**: لا تضع كلمات المرور أو connection strings في الكود

## الأوامر المطلوبة

```bash
# إنشاء Migration
dotnet ef migrations add InitialCreate

# تطبيق Migration
dotnet ef database update
```
