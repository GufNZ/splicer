using System;
using System.Collections.Generic;
using System.Linq;

public struct Match {
	public int FirstIndex;
	public int SecondIndex;
	public int SecondOffset;
	public int Score;
	public string Combined;
	
	public static Match NoMatch = new Match { Score = 0 };
	
	public override string ToString() {
		return (Score > 0)
			? string.Format("Match[{0}:{1}]@{2}+{3}='{4}'", FirstIndex, SecondIndex, SecondOffset, Score, Combined)
			: "NO_MATCH";
	}
}

public static class Program
{
	public static void Main(string[] args)
	{
		if (args == null) {
			args = new string[0];
		}

		IEnumerable<string> parts;
		string input = null;
		switch (args.Length) {
			case 0:
				input = "This is some sort of long string message that gets chopped.";
				parts = Chop(input, DateTime.Now.Millisecond, 20, 2, 4);
				OutputList(parts);
				break;

			case 1:
				RunTests(args[0]);
				return;

			default:
				parts = args;
				break;
		}

		var result = Merge(parts.ToArray());
		if (input == null) {
			Console.WriteLine(result);
		} else {
			Console.WriteLine("Was: '{0}'\nGot: '{1}'\nMatch = {2}", input, result, (input == result) ? "SUCCESS" : "FAIL!!!");
		}
	}
	
	public static string Merge(string[] inputParts) {
		if (inputParts.Length == 0) {
			return "";
		}


		var parts = inputParts.ToList();
		while (parts.Count > 1) {
			var match = FindBestOverlap(parts);
			//Console.WriteLine("\nMerge:\n" + parts[match.FirstIndex] + '\n' + new string(' ', match.SecondOffset) + parts[match.SecondIndex] + '\n' + match.Combined + '\n' + match.ToString());
			parts[match.FirstIndex] = match.Combined;
			parts.RemoveAt(match.SecondIndex);
		}
		return parts[0];
	}
	
	private static Match FindBestOverlap(List<string> parts) {
		var matches = new List<Match>();
		for (int i = 0; i < parts.Count - 1; i++) {
			for (int j = i + 1; j < parts.Count; j++) {
				var match = GetMatch(parts, i, j);
				//Console.WriteLine("Match[{0}][{1}] = {2}", i, j, match);
				if (match.Score > 0) {
					matches.Add(match);
				}
				match = GetMatch(parts, j, i);
				//Console.WriteLine("Match[{0}][{1}] = {2}", j, i, match);
				if (match.Score > 0) {
					matches.Add(match);
				}
			}
		}
		var bestMatch = matches.OrderBy(x => -x.Score).FirstOrDefault();
		if (bestMatch.Score > 0) {
			return bestMatch;
		}


		var concatMatch = new Match {
			FirstIndex = 0,
			SecondIndex = 1,
			Score = -1,
			Combined = string.Join("", parts)
		};
		parts.Clear();
		parts.Add("");
		parts.Add("");
		return concatMatch;
	}

	private static Match GetMatch(List<string> parts, int firstIndex, int secondIndex) {
		var first = parts[firstIndex];
		var second = parts[secondIndex];
		if (second.Length < first.Length && first.IndexOf(second) >= 0) {
			return new Match {
				Score = int.MaxValue,
				FirstIndex = firstIndex,
				SecondIndex = secondIndex,
				Combined = first
			};
		}
		
		char secondStart = second[0];
		for (var start = 0; start < first.Length; start++) {
			if (first[start] != secondStart) {
				continue;
			}

			
			var firstLength = first.Length - start;
			if (second.Length < firstLength) {
				if (first.Substring(start, second.Length) == second) {
					return new Match {
						Score = second.Length,
						FirstIndex = firstIndex,
						SecondIndex = secondIndex,
						SecondOffset = start,
						Combined = first
					};
				}
			} else if (first.Substring(start, firstLength) == second.Substring(0, firstLength)) {
				return new Match {
					Score = firstLength,
					FirstIndex = firstIndex,
					SecondIndex = secondIndex,
					SecondOffset = start,
					Combined = first.Substring(0, start) + second
				};
			}
		}
		return Match.NoMatch;
	}


	private static int testCount;
	public static void RunTests(string option) {
		Assert(new string[0], "");
		Assert(new[] { "abc", "bcd" }, "abcd");
		Assert(new[] { "ab", "bc" }, "abc");
		Assert(new[] { "ab", "cd" }, "abcd");
		Assert(new[] { "aba", "aca" }, "abaca");
		Assert(new[] { "abcdef", "cd" }, "abcdef");

		Console.WriteLine(testCount + " Passed");
	}

	private static void Assert(string[] parts, string expected) {
		if (Merge(parts) != expected) {
			OutputList(parts);
			throw new Exception("Didn't match expected: " + expected);
		}


		testCount++;
	}


	private static void OutputList(IEnumerable<string> list) {
		Console.WriteLine("['" + string.Join("', '", list) + "']");
	}
	
	private static void OutputList(IEnumerable<Tuple<int, string>> list) {
		foreach (var item in list) {
			Console.WriteLine(new string(' ', item.Item1) + item.Item2);
		}
	}


	public static List<string> Chop(string input, int seed, int cuts, int minOverlap, int maxOverlap) {
		var result = new List<Tuple<int, string>> {
			new Tuple<int, string>(0, input)
		};
		var random = new Random(seed);
		
		for (int i = 0; i < cuts; i++) {
			var index = result.Select((x, ix) => new { size = x.Item2.Length, index = ix }).OrderBy(x => -x.size).First().index;
			var toCut = result[index];

			var cut = random.Next(minOverlap, toCut.Item2.Length - minOverlap) + toCut.Item1;
			var start = Math.Max(0, cut - random.Next(minOverlap, maxOverlap));
			var end = Math.Min(input.Length, cut + random.Next(minOverlap, maxOverlap));

			var firstStart = toCut.Item1;
			var firstEnd = end;

			var secondStart = start;
			var secondEnd = toCut.Item1 + toCut.Item2.Length;
			
			var first = input.Substring(firstStart, firstEnd - firstStart);
			var second = input.Substring(secondStart, secondEnd - secondStart);

			var firstItem = new Tuple<int, string>(firstStart, first);
			var secondItem = new Tuple<int, string>(secondStart, second);
			//OutputList(new[] { result[index], firstItem, secondItem });
			result[index] = firstItem;
			result.Add(secondItem);
		}

		OutputList(result);
		
		return result.OrderBy(x => random.Next()).Select(x => x.Item2).ToList();
	}
}
