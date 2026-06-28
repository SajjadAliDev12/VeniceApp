namespace Venice.Admin.Models;

// هذا الكلاس يمثل البيانات القادمة من Get All
public class UserRow
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? EmailAddress { get; set; }

    
    public int Role { get; set; }
}

// هذا الكلاس يمثل البيانات المرسلة للإضافة والتعديل
public class UserUpsertDto
{
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? EmailAddress { get; set; }
    public int Role { get; set; }
    public string? Password { get; set; }
}