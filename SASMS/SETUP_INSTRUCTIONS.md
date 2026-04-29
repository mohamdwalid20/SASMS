# تعليمات الإعداد - SASMS

## الخطوات المطلوبة لتشغيل النظام

### 1. تثبيت الحزم المطلوبة
```bash
dotnet restore
```

### 2. إعداد قاعدة البيانات

#### تحديث Connection String
افتح `appsettings.json` وقم بتحديث Connection String:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=SASMSDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

#### إنشاء Migration
```bash
dotnet ef migrations add InitialCreate
```

#### تطبيق Migration على قاعدة البيانات
```bash
dotnet ef database update
```

### 3. تشغيل المشروع
```bash
dotnet run
```

أو من Visual Studio:
- اضغط F5 أو Run

## الميزات الأمنية المطبقة

✅ **تشفير كلمة المرور**: BCrypt مع Salt تلقائي
✅ **Authentication**: Cookie-based مع Session management
✅ **Authorization**: Role-based policies
✅ **Security Headers**: حماية من XSS و Clickjacking
✅ **CSRF Protection**: Anti-forgery tokens
✅ **Input Validation**: Server-side و Client-side

## أنواع المستخدمين

1. **Admin** - الإدارة (يتم إنشاؤه يدوياً في قاعدة البيانات)
2. **StudentAffairs** - شؤون الطلاب (يمكن التسجيل)
3. **Student** - الطلاب (يمكن التسجيل)
4. **Applicant** - المتقدمين (يمكن التسجيل)

## استخدام Authorization

### في Controllers:
```csharp
[Authorize] // يتطلب تسجيل دخول
public IActionResult ProtectedAction() { }

[Authorize(Policy = "AdminOnly")] // للإدارة فقط
public IActionResult AdminAction() { }

[Authorize(Policy = "StudentAffairsOnly")] // لشؤون الطلاب والإدارة
public IActionResult StudentAffairsAction() { }
```

### في Views:
```csharp
@if (User.Identity.IsAuthenticated)
{
    <p>مرحباً @User.Identity.Name</p>
}

@if (User.IsInRole("Admin"))
{
    <a href="/Admin">لوحة التحكم</a>
}
```

## ملاحظات مهمة

1. **أول مستخدم Admin**: يجب إنشاؤه يدوياً في قاعدة البيانات أو من خلال Console App
2. **HTTPS**: في الإنتاج، تأكد من تفعيل HTTPS
3. **Secrets**: استخدم User Secrets أو Azure Key Vault للبيانات الحساسة
4. **Logging**: يتم تسجيل جميع محاولات تسجيل الدخول في Logs

## استكشاف الأخطاء

### مشكلة في الاتصال بقاعدة البيانات
- تأكد من أن SQL Server يعمل
- تحقق من Connection String
- تأكد من أن قاعدة البيانات موجودة

### مشكلة في Migration
```bash
# حذف Migration الأخير
dotnet ef migrations remove

# إعادة إنشاء Migration
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### مشكلة في Authentication
- تأكد من أن Cookies مفعلة في المتصفح
- تحقق من أن Session middleware مضاف في Program.cs
