using System.ComponentModel.DataAnnotations;

namespace CPE_Review_Hub.Models;

public class AcademicLevel
{
    public int Id { get; set; }
    [MaxLength(40)] public string Name { get; set; } = "";
    public ICollection<Subject> Subjects { get; set; } = [];
}
