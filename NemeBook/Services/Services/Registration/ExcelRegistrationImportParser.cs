using ClosedXML.Excel;
using Services.Dtos.Registration;
using Services.Interfaces.Registration;

namespace Services.Services.Registration;

public class ExcelRegistrationImportParser : IRegistrationImportParser
{
    private static readonly string[] FirstNameHeaders = ["first name", "firstname", "име"];
    private static readonly string[] MiddleNameHeaders = ["middle name", "middlename", "презиме", "бащино име"];
    private static readonly string[] LastNameHeaders = ["last name", "lastname", "фамилия"];
    private static readonly string[] EmailHeaders = ["email", "e-mail", "имейл", "електронна поща"];
    private static readonly string[] BirthDateHeaders = ["birth date", "birthdate", "дата на раждане"];
    private static readonly string[] ClassLabelHeaders = ["class", "class label", "class name", "клас", "паралелка"];
    private static readonly string[] ParentEmailsHeaders = ["parent emails", "parentemails", "родителски имейли", "имейли на родители"];
    private static readonly string[] PhoneNumberHeaders = ["phone number", "phonenumber", "phone", "телефон", "телефонен номер"];
    private static readonly string[] SubjectsHeaders = ["subjects", "subjects taught", "предмети", "предмети, които преподава"];

    public Task<IReadOnlyList<StudentImportDto>> ParseStudentsAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = GetWorksheet(workbook, "Students", "Ученици");
        var rows = ReadRows(worksheet);

        IReadOnlyList<StudentImportDto> students = rows
            .Select(row => new StudentImportDto
            {
                RowNumber = row.RowNumber,
                FirstName = row.GetRequired(FirstNameHeaders),
                MiddleName = row.GetOptional(MiddleNameHeaders),
                LastName = row.GetRequired(LastNameHeaders),
                Email = row.GetRequired(EmailHeaders),
                BirthDate = row.GetDateOnly(BirthDateHeaders),
                PhoneNumber = row.GetOptional(PhoneNumberHeaders),
                ClassLabel = row.GetRequired(ClassLabelHeaders),
                ParentEmails = SplitValues(row.GetOptional(ParentEmailsHeaders))
            })
            .ToList();

        return Task.FromResult(students);
    }

    public Task<IReadOnlyList<TeacherImportDto>> ParseTeachersAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = GetWorksheet(workbook, "Teachers", "Учители");
        var rows = ReadRows(worksheet);

        IReadOnlyList<TeacherImportDto> teachers = rows
            .Select(row => new TeacherImportDto
            {
                RowNumber = row.RowNumber,
                FirstName = row.GetRequired(FirstNameHeaders),
                MiddleName = row.GetOptional(MiddleNameHeaders),
                LastName = row.GetRequired(LastNameHeaders),
                Email = row.GetRequired(EmailHeaders),
                BirthDate = row.GetDateOnly(BirthDateHeaders),
                PhoneNumber = row.GetOptional(PhoneNumberHeaders),
                Subjects = SplitValues(row.GetOptional(SubjectsHeaders))
            })
            .ToList();

        return Task.FromResult(teachers);
    }

    public Task<IReadOnlyList<ParentImportDto>> ParseParentsAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = GetWorksheet(workbook, "Parents", "Родители");
        var rows = ReadRows(worksheet);

        IReadOnlyList<ParentImportDto> parents = rows
            .Select(row => new ParentImportDto
            {
                RowNumber = row.RowNumber,
                Email = row.GetRequired(EmailHeaders)
            })
            .ToList();

        return Task.FromResult(parents);
    }

    private static IXLWorksheet GetWorksheet(XLWorkbook workbook, params string[] preferredNames)
    {
        foreach (var preferredName in preferredNames)
        {
            if (workbook.TryGetWorksheet(preferredName, out var worksheet))
            {
                return worksheet;
            }
        }

        return workbook.Worksheets.First();
    }

    private static IReadOnlyList<ExcelImportRow> ReadRows(IXLWorksheet worksheet)
    {
        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            return Array.Empty<ExcelImportRow>();
        }

        var headerRow = usedRange.FirstRowUsed();
        var headers = headerRow.CellsUsed()
            .Select(cell => new
            {
                ColumnNumber = cell.Address.ColumnNumber,
                Header = NormalizeHeader(cell.GetString())
            })
            .Where(header => !string.IsNullOrWhiteSpace(header.Header))
            .GroupBy(header => header.Header)
            .ToDictionary(group => group.Key, group => group.First().ColumnNumber);

        return usedRange.RowsUsed()
            .Skip(1)
            .Where(row => !row.CellsUsed().All(cell => string.IsNullOrWhiteSpace(cell.GetString())))
            .Select(row => new ExcelImportRow(row.RowNumber(), headers, row))
            .ToList();
    }

    private static IReadOnlyCollection<string> SplitValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value.Split([';', ',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string NormalizeHeader(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private sealed class ExcelImportRow
    {
        private readonly IReadOnlyDictionary<string, int> headers;
        private readonly IXLRangeRow row;

        public ExcelImportRow(int rowNumber, IReadOnlyDictionary<string, int> headers, IXLRangeRow row)
        {
            RowNumber = rowNumber;
            this.headers = headers;
            this.row = row;
        }

        public int RowNumber { get; }

        public string GetRequired(params string[] headerNames)
        {
            return GetOptional(headerNames) ?? string.Empty;
        }

        public string? GetOptional(params string[] headerNames)
        {
            var cell = GetCell(headerNames);
            if (cell is null)
            {
                return null;
            }

            var value = cell.GetString();
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        public DateOnly GetDateOnly(params string[] headerNames)
        {
            var cell = GetCell(headerNames);
            if (cell is null || cell.IsEmpty())
            {
                return default;
            }

            if (cell.DataType == XLDataType.DateTime)
            {
                return DateOnly.FromDateTime(cell.GetDateTime());
            }

            return DateOnly.TryParse(cell.GetString(), out var value)
                ? value
                : default;
        }

        private IXLCell? GetCell(params string[] headerNames)
        {
            foreach (var headerName in headerNames.Select(NormalizeHeader))
            {
                if (headers.TryGetValue(headerName, out var columnNumber))
                {
                    return row.Cell(columnNumber);
                }
            }

            return null;
        }
    }
}
