namespace CPE_Review_Hub.Services.Curriculum;

public static class CurriculumCatalog
{
    private static readonly IReadOnlyDictionary<string, CurriculumCourse> Courses =
        Level100Curriculum.Build().Concat(Level200Curriculum.Build())
            .Concat(Level300Curriculum.Build()).Concat(Level400Curriculum.Build())
            .ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

    public static CurriculumCourse Get(string code) => Courses.TryGetValue(code, out var course)
        ? course : throw new InvalidOperationException($"No curriculum is defined for {code}.");

    public static IReadOnlyCollection<CurriculumCourse> All => Courses.Values.ToList();
}
