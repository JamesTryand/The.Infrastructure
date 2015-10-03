using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using Infrastructure;

namespace Infrastructure.Testing
{
	public class InTheTestsFor<TAggregate> where TAggregate : AggregateRoot, new()
	{
		public string Role { get; private set; }
		public string Feature { get; private set; }
		public string Benefit { get; private set; }

        public List<Test<TAggregate>> _tests = null;
        public List<Test<TAggregate>> Tests
        {
            get
            {
                if (this._tests == null) { _tests = new List<Test<TAggregate>>(); }
                return _tests;
            }
            private set
            {
                this._tests = value;
            }
        }
        public Test<TAggregate> _currentTest = null;
        public Test<TAggregate> CurrentTest
        {
            get
            {
                if (this._currentTest == null) { _currentTest = new Test<TAggregate>(); }
                return _currentTest;
            }
            private set
            {
                this._currentTest = value;
            }
        }



        public static InTheTestsFor<TAggregate> AsA(string role) {
			var v = new InTheTestsFor<TAggregate>();
			v.Role = role;
			return v;
		}
		public InTheTestsFor<TAggregate> SoThat(string benefit) {
			this.Benefit = benefit;
			return this;
		}
		
		public InTheTestsFor<TAggregate> IWant(string feature) {
			this.Feature = feature;
			return this;
		}
		
		public InTheTestsFor<TAggregate> For() {
			return this;
		}
		
		public InTheTestsFor<TAggregate> Given(params Event[] startingEvents) {
			this.CurrentTest.Arrange = startingEvents ?? new Event[0];
			AssignTests();
			return this;
		}
		
		public InTheTestsFor<TAggregate> When(Expression<Action<TAggregate>> test){
			this.CurrentTest.Act = test;
			AssignTests();
			return this;
		}
		
		public InTheTestsFor<TAggregate> Then(Func<InTheTestsFor<TAggregate>,IEnumerable<Expression<Func<IEnumerable<Event>,bool>>>> tests) {
			this.CurrentTest.Assert = tests(this);
			AssignTests();
			return this;
		}
		
		private void AssignTests() {
			var result = UpdateTests(this.Tests, this.CurrentTest);
			this.CurrentTest = result.Item1;
			this.Tests = result.Item2;
		}
		
		// Only prints out failed tests.
		public bool Results() {
			var results = Tests.Select(Test => new{ Test, TestResults = Test.TestsPass() });
			
			results.ForEach(result => {
				var canprint = result.TestResults.Select(t => t.Item1).Any(t => t);
				if (canprint) {
					Debug.WriteLine(result.Test.ToString());
				}				
				result.TestResults.Where(v => v.Item1).Select(
					value => 
					(value.Item1 
					? "PASSED"
					: "FAILED") + "\t" + 
					value.Item2)
					.ForEach(i => Debug.WriteLine(i));
			});
			return results.SelectMany(i => i.TestResults.Select(j => j.Item1)).All(i => i);
		}
		
		// Prints out all tests
		public bool FullResults() {
			
			var results = Tests.Select(Test => new{ Test, TestResults = Test.TestsPass() });
			results.ForEach(result => {
				Debug.WriteLine(result.Test.ToString());
				result.TestResults.Select(
					value => 
					(value.Item1 
					? "PASSED"
					: "FAILED") + "\t" + 
					value.Item2)
					.ForEach(i => Debug.WriteLine(i));
			});
			return results.SelectMany(i => i.TestResults.Select(j => j.Item1)).All(i => i);
		}
		
		public IEnumerable<Expression<Func<IEnumerable<Event>,bool>>> ExpectTheFollowingEvents(params Expression<Func<IEnumerable<Event>,bool>>[] conditions) {
			foreach (var condition in conditions)
			{
				yield return condition;
			}
		} 
		
		public override string ToString() {
			return new[] { Role, Feature, Benefit}.Any(i => string.IsNullOrWhiteSpace(i))
			? base.ToString()
			: string.Format(
				string.Join(Environment.NewLine, 
				new [] {"As a {0} ", "So that {1}", "I want {2}"}),
				Role, Feature, Benefit);
		}
		
		private bool IsComplete(Test<TAggregate> test) {
			return new object [] { test.Arrange, test.Act, test.Assert }.All(x => x != null) && 
				new IEnumerable<object>[] { test.Arrange, test.Assert }.All(x => !x.IsEmpty());
		}
		
		private Tuple<Test<TAggregate>,List<Test<TAggregate>>> UpdateTests(List<Test<TAggregate>> tests, Test<TAggregate> test) {
			Test<TAggregate> returnTest;
			if (IsComplete(test))
			{
				tests.Add(test);
				returnTest = new Test<TAggregate>();
			} else {
				returnTest = test;
			}
			return Tuple.Create(returnTest, tests);
		}
	}
	
	
	
	
	
	public class Test<TAggregate> where TAggregate : AggregateRoot, new() {
		public IEnumerable<Event> Arrange { get; set; }
		public Expression<Action<TAggregate>> Act { get; set; }
		public IEnumerable<Expression<Func<IEnumerable<Event>,bool>>> Assert { get; set; }
		
		public IEnumerable<Tuple<bool,string>> TestsPass() {
			var sut = new TAggregate();
			sut.LoadsFromHistory(Arrange);
			Act.Compile().Invoke(sut);
			var results = sut.GetUncommittedChanges();
			foreach (var test in Assert)
			{
				yield return IndividualTestPasses(test, results);
			}
		}
		
		private Tuple<bool,string> IndividualTestPasses(Expression<Func<IEnumerable<Event>,bool>> test, IEnumerable<Event> results) {
			var testText = test.ToString();
			var testResult = test.Compile().Invoke(results);
			return Tuple.Create(testResult, string.Join(Environment.NewLine, testText, "Applied with", JsonConvert.SerializeObject(results)));
		}
		
		public override string ToString() {
			var sb = new StringBuilder();
			sb.AppendLine(JsonConvert.SerializeObject(this.Arrange));
			sb.Append(this.Act.ToString());
			foreach (var test in this.Assert)
			{
				sb.Append(test.ToString());
			}
			return sb.ToString();
		}
	}
	
	//  public class Scenario<TEntity> {
	//  	public Event[] Given { get; private set; }
	//  	public Expression<Action<TEntity>> When { get; private set; }
	//  	public Expression<Func<Event[],bool>>[] Then { get; private set; }
	//  	
	//  	public Scenario(Event[] Given, Expression<Action<TEntity>> When, params Expression<Func<Event[],bool>>[] Then) {
	//  		this.Given = Given;
	//  		this.When = When;
	//  		this.Then = Then;
	//  	}
	//  }
	//  
    //  public interface Story {
    //      string Role { get; }
    //      string Feature { get; }
    //      string Benefit { get; }
    //  } 
	//  
	//  public class ReportWriter
	//  {
	//  	public string WriteReport<T>(Story story = null, params Scenario<T>[] scenarios) {
	//  		var content = new StringBuilder();
	//  		foreach (var scenario in scenarios)
	//  		{
	//  			
	//  		}
	//  		return content.ToString();
	//  	}
	//  	
	//  	public string TestScenario<T>(Scenario<T> scenario) {
	//  		
	//  	}
	//  }
}