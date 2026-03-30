using Xunit;
using NSubstitute;
using System.Threading;
using FluentAssertions;
using Arc.Domain.Models;
using System.Threading.Tasks;
using Arc.Application.Results;
using Arc.Application.Execution;
using Arc.Application.RegressionGates;
using Arc.Infrastructure.RegressionGates;


namespace Arc.UnitTests.Execution
{
    public sealed class DeterministicRegressionGateServiceTests
    {
        [Fact]
        public async Task RunGateAsync_WithMatchingCandidate_ShouldReturnPassedResult()
        {
            // Arrange
            var gateId = new RegressionGateId(Guid.NewGuid());
            var candidateExecutionId = "candidate-exec-1";
            var gate = new RegressionGate(
                gateId,
                UserId.Anonymous,
                "Test Gate",
                new GoldenExecutionId("golden-exec-1"),
                new[] { new DivergenceRule(DivergenceRuleType.SimilarityPercentage, 1.0) }
            );
            var repository = Substitute.For<IRegressionGateRepository>();
            repository.GetByIdAsync(gateId, Arg.Any<CancellationToken>()).Returns(gate);

            var tasks = new[] { new TaskExecutionResult("task-1", "Task 1", 1, TaskExecutionStatus.Succeeded, "output") };
            var executionStore = Substitute.For<IExecutionResultStore>();
            executionStore.GetAsync("golden-exec-1").Returns(new ExecutionResult(UserId.Anonymous, tasks));
            executionStore.GetAsync(candidateExecutionId).Returns(new ExecutionResult(UserId.Anonymous, tasks));

            var comparison = new ExecutionComparisonResult(
                "golden-exec-1",
                candidateExecutionId,
                new List<TaskComparisonItem>(),
                new ExecutionDiffMetrics(1, 1, 0, -1, true, true, 1.0),
                "Identical");
            var comparer = Substitute.For<IExecutionComparer>();
            comparer.CompareAsync("golden-exec-1", candidateExecutionId).Returns(comparison);

            var criticalPath = new CriticalPathAnalysis(new List<string>(), 0, 0);
            var resourceUtil = new ResourceUtilizationMetrics(0, 0, 0, 0, 0, 0);
            var profile = new ExecutionPerformanceProfile("golden-exec-1", new List<TaskPerformanceMetrics>(), criticalPath, resourceUtil, DateTime.UtcNow);
            var profiler = Substitute.For<IExecutionProfiler>();
            profiler.GenerateProfileAsync(Arg.Any<string>()).Returns(profile);

            var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<DeterministicRegressionGateService>>();
            var service = new DeterministicRegressionGateService(repository, executionStore, comparer, profiler, logger);

            // Act
            var result = await service.RunGateAsync(gateId, candidateExecutionId);

            // Assert
            result.GateId.Should().Be(gateId);
            result.CandidateExecutionId.Should().Be(candidateExecutionId);
            result.Passed.Should().BeTrue();
        }

        [Fact]
        public async Task RunGateAsync_WithMissingGate_ShouldThrow()
        {
            // Arrange
            var gateId = new RegressionGateId(Guid.NewGuid());
            var repository = Substitute.For<IRegressionGateRepository>();
            repository.GetByIdAsync(gateId, Arg.Any<CancellationToken>()).Returns((RegressionGate?)null);
            var executionStore = Substitute.For<IExecutionResultStore>();
            var comparer = Substitute.For<IExecutionComparer>();
            var profiler = Substitute.For<IExecutionProfiler>();
            var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<DeterministicRegressionGateService>>();
            var service = new DeterministicRegressionGateService(repository, executionStore, comparer, profiler, logger);

            // Act
            var act = async () => await service.RunGateAsync(gateId, "candidate-exec-1");

            // Assert
            await act.Should().ThrowAsync<RegressionGateInvalidException>();
        }

        [Fact]
        public async Task RunGateAsync_WithMissingCandidateExecution_ShouldThrow()
        {
            // Arrange
            var gateId = new RegressionGateId(Guid.NewGuid());
            var gate = new RegressionGate(
                gateId,
                UserId.Anonymous,
                "Test Gate",
                new GoldenExecutionId("golden-exec-1"),
                new[] { new DivergenceRule(DivergenceRuleType.SimilarityPercentage, 1.0) }
            );
            var repository = Substitute.For<IRegressionGateRepository>();
            repository.GetByIdAsync(gateId, Arg.Any<CancellationToken>()).Returns(gate);
            var executionStore = Substitute.For<IExecutionResultStore>();
            executionStore.GetAsync("golden-exec-1").Returns(new ExecutionResult(UserId.Anonymous, Array.Empty<TaskExecutionResult>()));
            executionStore.GetAsync("missing-candidate").Returns((ExecutionResult?)null);
            var comparer = Substitute.For<IExecutionComparer>();
            var profiler = Substitute.For<IExecutionProfiler>();
            var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<DeterministicRegressionGateService>>();
            var service = new DeterministicRegressionGateService(repository, executionStore, comparer, profiler, logger);

            // Act
            var act = async () => await service.RunGateAsync(gateId, "missing-candidate");

            // Assert
            await act.Should().ThrowAsync<RegressionGateInvalidException>();
        }
    }
}