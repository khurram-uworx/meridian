using System.Threading.Tasks;
using Uworx.Meridian.CourseSource;
using Uworx.Meridian.Entities;

namespace Uworx.Meridian;

public interface IEnrollmentService
{
    Task<Enrollment> EnrollAsync(string learnerEmail, CourseSourceLocator source);
}
