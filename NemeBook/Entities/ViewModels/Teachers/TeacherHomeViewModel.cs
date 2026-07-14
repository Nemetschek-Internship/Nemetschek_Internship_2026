namespace Entities.ViewModels.Teachers;

public class TeacherHomeViewModel
{
    public string TeacherName { get; set; } = string.Empty;

    public string TeacherInitials { get; set; } = string.Empty;

    public string MainMeta { get; set; } = "Учител";

    public Guid? ClassId { get; set; }

    public Guid? SubjectId { get; set; }

    public string ClassName { get; set; } = "0";

    public int StudentCount { get; set; }

    public IReadOnlyList<TeacherClassListItem> TeachingClasses { get; set; } = Array.Empty<TeacherClassListItem>();

    public IReadOnlyList<TeacherStudentListItem> Students { get; set; } = Array.Empty<TeacherStudentListItem>();

    public TeacherSelectedStudentViewModel SelectedStudent { get; set; } = new();
}

public class TeacherClassListItem
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string SubjectNames { get; set; } = string.Empty;

    public int StudentCount { get; set; }

    public bool IsMainClass { get; set; }
}

public class TeacherStudentListItem
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Initials { get; set; } = string.Empty;

    public int RecordCount { get; set; }

    public bool IsSelected { get; set; }
}

public class TeacherSelectedStudentViewModel
{
    public Guid? Id { get; set; }

    public string FullName { get; set; } = "0";

    public string Initials { get; set; } = "0";

    public string ClassName { get; set; } = "0";

    public int RecordCount { get; set; }
}
