using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;

namespace NClassifier
{
	/// <summary>
	/// An implementation of a text classifier based on Bayes' algorithm.
	/// </summary>
	public class BayesianClassifier
	{
		private const double Cutoff = .9d;

		private static readonly HashSet<string> _stopWords = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
		{
			"a", "and", "the", "me", "i", "of", "if", "it", 
			"is", "they", "there", "but", "or", "to", "this", "you", 
			"in", "your", "on", "for", "as", "are", "that", "with",
			"have", "be", "at", "or", "was", "so", "out", "not", "an"
		};

		private readonly Dictionary<string, WordProbability> _words = new Dictionary<string, WordProbability>(StringComparer.InvariantCultureIgnoreCase);

		private static readonly Regex _tokenizeRegex = new Regex(@"\W", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

		/// <summary>
		/// Determines whether the specified input matches.
		/// </summary>
		/// <param name="input">The input.</param>
		/// <returns><c>true</c> if the specified input matches; otherwise, <c>false</c>.</returns>
		public bool IsMatch(string input)
		{
			return IsMatch(Tokenize(input));
		}

		/// <summary>
		/// Classifies the specified input.
		/// </summary>
		/// <param name="input">The input.</param>
		/// <returns>The proximity.</returns>
		public double Classify(string input)
		{
			Contract.Requires<ArgumentNullException>(input != null, "input");

			return Classify(Tokenize(input));
		}

		/// <summary>
		/// Classifies the specified words.
		/// </summary>
		/// <param name="words">The words.</param>
		/// <returns>The proximity.</returns>
		public double Classify(string[] words)
		{
			var wordProbabilities = CalculateWordsProbability(words);
			return NormalizeSignificance(CalculateOverallProbability(wordProbabilities));
		}

		/// <summary>
		/// Teaches a matching input.
		/// </summary>
		/// <param name="input">The input.</param>
		public void TeachMatch(string input)
		{
			Contract.Requires<ArgumentNullException>(input != null, "input");

			TeachMatch(Tokenize(input));
		}

		/// <summary>
		/// Teaches a non-matching input.
		/// </summary>
		/// <param name="input">The input.</param>
		public void TeachNonMatch(string input)
		{
			Contract.Requires<ArgumentNullException>(input != null, "input");

			TeachNonMatch(Tokenize(input));
		}

		/// <summary>
		/// Determines whether the specified input matches.
		/// </summary>
		/// <param name="input">The input.</param>
		/// <returns><c>true</c> if the specified input matches; otherwise, <c>false</c>.</returns>
		public bool IsMatch(string[] input)
		{
			Contract.Requires<ArgumentNullException>(input != null, "input");

			var matchProbability = Classify(input);

			return matchProbability >= Cutoff;
		}

		/// <summary>
		/// Teaches matching inputs.
		/// </summary>
		/// <param name="words">The words.</param>
		public void TeachMatch(string[] words)
		{
			for (var i = 0; i <= words.Length - 1; i++)
			{
				if (IsClassifiableWord(words[i]))
				{
					var word = words[i];
					WordProbability wordProbability;
					if (_words.TryGetValue(word, out wordProbability))
					{
						wordProbability.MatchingCount++;
					}
					else
					{
						_words.Add(word, new WordProbability(word, 1, 0));
					}
				}
			}
		}

		/// <summary>
		/// Teaches non-matching inputs.
		/// </summary>
		/// <param name="words">The words.</param>
		public void TeachNonMatch(string[] words)
		{
			for (var i = 0; i <= words.Length - 1; i++)
			{
				if (IsClassifiableWord(words[i]))
				{
					string word = words[i];
					WordProbability wordProbability;
					if (_words.TryGetValue(word, out wordProbability))
					{
						wordProbability.NonMatchingCount++;
					}
					else
					{
						_words.Add(word, new WordProbability(word, 0, 1));
					}
				}
			}
		}

		/// <summary>
		/// Calculates the overall probability.
		/// </summary>
		/// <param name="wordProbabilities">The WPS.</param>
		/// <returns>The proximity.</returns>
		public double CalculateOverallProbability(IList<WordProbability> wordProbabilities)
		{
			if (wordProbabilities == null || wordProbabilities.Count == 0)
			{
				return WordProbability.NeutralProbability;
			}

			// we need to calculate xy/(xy + z) where z = (1 - x)(1 - y)
			// first calculate z and xy
			var z = 0d;
			var xy = 0d;

			for (var i = 0; i < wordProbabilities.Count; i++)
			{
				z = z == 0 ? 1 - wordProbabilities[i].Probability : z * (1 - wordProbabilities[i].Probability);
				xy = xy == 0 ? wordProbabilities[i].Probability : xy * wordProbabilities[i].Probability;
			}

			return xy / (xy + z);
		}

		/// <summary>
		/// Calculates the words probability.
		/// </summary>
		/// <param name="words">The words.</param>
		/// <returns>The list of <see cref="WordProbability"/> instances.</returns>
		private IList<WordProbability> CalculateWordsProbability(string[] words)
		{
			if (words == null)
			{
				return new WordProbability[0];
			}
			
			var wordProbabilities = new List<WordProbability>();
			for (var i = 0; i < words.Length; i++)
			{
				if (IsClassifiableWord(words[i]))
				{
					WordProbability wordProbability;
					_words.TryGetValue(words[i], out wordProbability);

					if (wordProbability != null)
					{
						wordProbabilities.Add(wordProbability);
					}
				}
			}

			return wordProbabilities;
		}

		/// <summary>
		/// Determines whether the specified <paramref name="word"/> is classifiable.
		/// </summary>
		/// <param name="word">The word.</param>
		/// <returns><c>true</c> if the specified word is classifiable; otherwise, <c>false</c>.</returns>
		private bool IsClassifiableWord(string word)
		{
			return !string.IsNullOrEmpty(word) && !IsStopWord(word);
		}

		/// <summary>
		/// Normalizes the significance.
		/// </summary>
		/// <param name="significance">The significance.</param>
		/// <returns>The significance.</returns>
		public static double NormalizeSignificance(double significance)
		{
			if (WordProbability.UpperBound < significance)
			{
				return WordProbability.UpperBound;
			}

			if (WordProbability.LowerBound > significance)
			{
				return WordProbability.LowerBound;
			}

			return significance;
		}

		/// <summary>
		/// Determines whether the specified <paramref name="word"/> is a stop word.
		/// </summary>
		/// <param name="word">The word.</param>
		/// <returns><c>true</c> if the specified word is a stop word; otherwise, <c>false</c>.</returns>
		private static bool IsStopWord(string word)
		{
			return !string.IsNullOrEmpty(word) && _stopWords.Contains(word);
		}

		/// <summary>
		/// Tokenizes the specified input.
		/// </summary>
		/// <param name="input">The input.</param>
		/// <returns>The tokens.</returns>
		private static string[] Tokenize(string input)
		{
			return input == null ? new string[0] : _tokenizeRegex.Split(input);
		}
	}
}