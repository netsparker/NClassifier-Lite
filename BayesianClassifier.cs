using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;

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

		private static readonly WordProbability[] _emptyWordProbability = new WordProbability[0];

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
		public double Classify(IEnumerable<string> words)
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
			Contract.Requires<ArgumentNullException>(input != null, nameof(input));

			TeachMatch(Tokenize(input));
		}

		/// <summary>
		/// Teaches a non-matching input.
		/// </summary>
		/// <param name="input">The input.</param>
		public void TeachNonMatch(string input)
		{
			Contract.Requires<ArgumentNullException>(input != null, nameof(input));

			TeachNonMatch(Tokenize(input));
		}

		/// <summary>
		/// Determines whether the specified input matches.
		/// </summary>
		/// <param name="input">The input.</param>
		/// <returns><c>true</c> if the specified input matches; otherwise, <c>false</c>.</returns>
		public bool IsMatch(IEnumerable<string> input)
		{
			Contract.Requires<ArgumentNullException>(input != null, nameof(input));

			var matchProbability = Classify(input);

			return matchProbability >= Cutoff;
		}

		/// <summary>
		/// Teaches matching inputs.
		/// </summary>
		/// <param name="words">The words.</param>
		public void TeachMatch(IEnumerable<string> words)
		{
			foreach (var word in words)
			{
				if (IsClassifiableWord(word))
				{
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
		public void TeachNonMatch(IEnumerable<string> words)
		{
			foreach (var word in words)
			{
				if (IsClassifiableWord(word))
				{
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
				var probability = wordProbabilities[i].CalculateProbability();

				z = z == 0 ? 1 - probability : z * (1 - probability);
				xy = xy == 0 ? probability : xy * probability;
			}

			return xy / (xy + z);
		}

		/// <summary>
		/// Calculates the words probability.
		/// </summary>
		/// <param name="words">The words.</param>
		/// <returns>The list of <see cref="WordProbability"/> instances.</returns>
		private IList<WordProbability> CalculateWordsProbability(IEnumerable<string> words)
		{
			if (words == null)
			{
				return _emptyWordProbability;
			}

			var wordProbabilities = new List<WordProbability>();

			foreach (var word in words)
			{
				if (IsClassifiableWord(word))
				{
					WordProbability wordProbability;
					_words.TryGetValue(word, out wordProbability);

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

		public static bool IsTokenChar(char c)
		{
			return char.IsLetterOrDigit(c) || CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.ConnectorPunctuation || CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark;
		}

		public static IEnumerable<string> Tokenize(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				yield break;
			}

			var start = 0;
			var length = 0;

			for (int i = 0; i < text.Length; i++)
			{
				var c = text[i];

				if (IsTokenChar(c))
				{
					++length;
				}
				else
				{
					if (length == 0)
					{
						++start;
					}
					else
					{
						var word = string.Intern(text.Substring(start, length));

						yield return word;

						start += length + 1;
						length = 0;
					}
				}
			}

			if (length > 0)
			{
				var word = string.Intern(text.Substring(start, length));

				yield return word;
			}
		}
	}
}