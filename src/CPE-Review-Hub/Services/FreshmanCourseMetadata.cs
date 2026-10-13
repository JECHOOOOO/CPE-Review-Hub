namespace CPE_Review_Hub.Services;

// Display-only metadata until the reviewed Subjects migration can be applied
// by an Aiven account with ALTER permission. No values are written to other
// lesson or student records.
public static class FreshmanCourseMetadata
{
    public static (decimal? Units, int? Semester) ForCode(string code) => code switch
    {
        "CPE 101" or "CPE 102" => (3.0m, 1),
        "CPE 103" or "CPE 104" => (3.0m, 2),
        _ => (null, null)
    };
}
