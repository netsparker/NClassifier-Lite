using System;

namespace NClassifier
{
	/// <summary>
	/// Represents the probability of a particular word.
	/// </summary>
	[Serializable]
	public class WordProbability : IComparable
	{
		/// <summary>
		/// Default value to use if the implementation cannot work out how well a string matches.
		/// </summary>
		public const double NeutralProbability = .5d;

		/// <summary>
		/// The minimum likelyhood that a string matches.
		/// </summary>
		public const double LowerBound = .01d;

		/// <summary>
		/// The maximum likelyhood that a string matches.
		/// </summary>
		public const double UpperBound = .99d;

		private readonly string _word;

		private int _matchingCount;

		private int _nonMatchingCount;

		private double _probability = NeutralProbability;

		/// <summary>
		/// Gets or sets the matching count.
		/// </summary>
		public int MatchingCount 
		{
			get
			{
				return _matchingCount; 
			}

			set
			{
				_matchingCount = value; 
				CalculateProbability();
			}
		}

		/// <summary>
		/// Gets or sets the non matching count.
		/// </summary>
		public int NonMatchingCount
		{
			get
			{
				return _nonMatchingCount;
			}

			set
			{
				_nonMatchingCount = value;
				CalculateProbability();
			}
		}

		/// <summary>
		/// Gets the probability.
		/// </summary>
		public double Probability 
		{
			get { return _probability; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WordProbability"/> class.
		/// </summary>
		/// <param name="word">The word.</param>
		/// <param name="matchingCount">The matching count.</param>
		/// <param name="nonMatchingCount">The non matching count.</param>
		public WordProbability(string word, int matchingCount, int nonMatchingCount)
		{
			_word = word;
			_matchingCount = matchingCount;
			_nonMatchingCount = nonMatchingCount;
			CalculateProbability();
		}

		/// <summary>
		/// Calculates the probability.
		/// </summary>
		private void CalculateProbability()
		{
			double result;

			if (_matchingCount == 0)
			{
				result = _nonMatchingCount == 0 ? NeutralProbability : LowerBound;
			}
			else
			{
				result = BayesianClassifier.NormalizeSignificance((double)_matchingCount / (_matchingCount + _nonMatchingCount));
			}

			_probability = result;
		}

		/// <summary>
		/// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
		/// </summary>
		/// <param name="obj">An object to compare with this instance.</param>
		/// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="obj" /> in the sort order. Zero This instance occurs in the same position in the sort order as <paramref name="obj" />. Greater than zero This instance follows <paramref name="obj" /> in the sort order.</returns>
		/// <exception cref="InvalidCastException">Thrown when specified <paramref name="obj"/> is not a <see cref="WordProbability"/> instance.</exception>
		public int CompareTo(object obj)
		{
			if (!(obj is WordProbability))
			{
				throw new InvalidCastException(string.Format("{0} is not a {1}", obj.GetType(), GetType()));
			}

			var rhs = (WordProbability)obj;

			if (_word != rhs._word)
			{
				return _word.CompareTo(rhs._word);
			}
			
			return 0;
		}
	}
}