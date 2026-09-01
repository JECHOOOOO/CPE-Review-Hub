namespace CPE_Review_Hub.Services.Curriculum;

public static class Level300Curriculum
{
    public static IReadOnlyList<CurriculumCourse> Build() =>
        Level300CurriculumA.Build().Concat(Level300CurriculumC.Build())
            .Concat(Level300CurriculumB.Build()).Concat(Level300CurriculumD.Build()).ToList();
}
