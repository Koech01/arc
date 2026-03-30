using Arc.Domain.Models;

namespace Arc.Application.Planning;

public interface IPlanner
{
    ExecutionGraph Plan(string input);
}