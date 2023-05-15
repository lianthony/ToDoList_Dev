using System;
using System.Diagnostics;
using System.Collections.Generic;

using Abstractspoon.Tdl.PluginHelpers;

namespace DayViewUIExtension
{

	// --------------------------------------------------------------

	public class TimeBlocks
	{
		private Dictionary<uint, TimeBlockSeriesList> m_TaskSeries = new Dictionary<uint, TimeBlockSeriesList>();

		public TimeBlockSeriesList GetTaskSeries(uint taskId, bool autoCreate)
		{
			TimeBlockSeriesList seriesList = null;

			if ((taskId != 0) && !m_TaskSeries.TryGetValue(taskId, out seriesList) && autoCreate)
			{
				seriesList = new TimeBlockSeriesList(taskId);
				m_TaskSeries[taskId] = seriesList;
			}

			return seriesList;
		}

		public void Load(Preferences prefs, String key)
		{
			string prefsKey = (key + "\\TimeBlocks");

			int count = prefs.GetProfileInt(prefsKey, "Count", 0);
			m_TaskSeries.Clear();

			for (int i = 0; i < count; i++)
			{
				var entry = string.Format("Task{0}", i);
				var value = prefs.GetProfileString(prefsKey, entry, "");

				var seriesList = TimeBlockSeriesList.Decode(value);

				if (seriesList != null)
					m_TaskSeries[seriesList.TaskId] = seriesList;
			}
		}

		public void Save(Preferences prefs, String key)
		{
			string prefsKey = (key + "\\TimeBlocks");
			prefs.DeleteProfileSection(prefsKey, true);

			prefs.WriteProfileInt(prefsKey, "Count", m_TaskSeries.Count);

			int i = 0;
			foreach (var task in m_TaskSeries)
			{
				var value = task.Value.Encode();

				if (!string.IsNullOrWhiteSpace(value))
				{
					var entry = string.Format("Task{0}", i++);
					prefs.WriteProfileString(prefsKey, entry, value);
				}
			}
		}

		public bool SynchroniseDates(TaskItem taskItem)
		{
			Debug.Assert(taskItem != null);

			if (taskItem.IsDone)
				return false;

			var taskSeries = GetTaskSeries(taskItem.Id, false);

			if (taskSeries == null)
				return false;

			return taskSeries.SynchroniseDates(taskItem);
		}
	}

	// --------------------------------------------------------------

	public class TimeBlockSeriesList
	{
		private List<TimeBlockSeries> m_Series = new List<TimeBlockSeries>();

		// ----------------------------

		public IEnumerable<TimeBlockSeries> Series { get { return m_Series; } }
		public uint TaskId { get; private set; }

		// ----------------------------

		public TimeBlockSeriesList(uint taskId)
		{
			Debug.Assert(taskId != 0);

			TaskId = taskId;
		}

		public bool AddSeries(TimeBlockSeriesAttributes attribs)
		{
			var series = TimeBlockSeries.Create(attribs);

			if (series == null)
				return false;

			m_Series.Add(series);
			return true;
		}

		public bool SynchroniseDates(TaskItem taskItem)
		{
			Debug.Assert(taskItem.Id == TaskId);

			bool synced = false;

			foreach (var series in m_Series)
				synced |= series.SynchroniseDates(taskItem);

			return synced;
		}

		public bool DuplicateBlock(TimeBlock block)
		{
			foreach (var series in m_Series)
			{
				if (series.DuplicateTimeBlock(block))
					return true;
			}

			return false;
		}

		public TimeBlockSeries GetSeries(TimeBlock block)
		{
			foreach (var series in m_Series)
			{
				if (series.Contains(block))
					return series;
			}

			return null;
		}

		public bool RemoveBlock(TimeBlock block)
		{
			foreach (var series in m_Series)
			{
				if (series.RemoveTimeBlock(block))
				{
					if (series.BlockCount == 0)
						m_Series.Remove(series);

					return true;
				}
			}

			return false;
		}

		public bool RemoveSeries(TimeBlock block)
		{
			foreach (var series in m_Series)
			{
				if (series.RemoveTimeBlock(block))
				{
					m_Series.Remove(series);
					return true;
				}
			}

			return false;
		}

		public string Encode()
		{
			string seriesList = string.Empty;

			foreach (var series in m_Series)
				seriesList = seriesList + series.Encode() + '+';

			if (!string.IsNullOrWhiteSpace(seriesList))
				return string.Format("{0}={1}", TaskId, seriesList.TrimEnd('+'));
			
			return string.Empty;
		}

		static public TimeBlockSeriesList Decode(string blocks)
		{
			var items = blocks.Split('=');
			uint taskId = 0;

			if (items.Length != 2)
				return null;

			if (!uint.TryParse(items[0], out taskId) || (taskId == 0))
				return null;
			
			items = items[1].Split('+');

			if (items.Length == 0)
				return null;

			var seriesList = new TimeBlockSeriesList(taskId);

			foreach (var item in items)
			{
				var series = TimeBlockSeries.Decode(item);

				if (series != null)
					seriesList.m_Series.Add(series);
			}

			if (seriesList.m_Series.Count == 0)
				return null;
			
			return seriesList;
		}

	}

	// --------------------------------------------------------------

	public class TimeBlockSeriesAttributes
	{
		public int m_DaysOfWeek = DateUtil.MapDaysOfWeek(DateUtil.DaysOfWeek());

		// ---------------------------------

		public DateTime FromDate = DateTime.MinValue;
		public DateTime ToDate = DateTime.MaxValue;
		public TimeSpan FromTime;
		public TimeSpan ToTime;
		public bool SyncToTaskDates = false;

		public List<DayOfWeek> DaysOfWeek
		{
			get { return DateUtil.MapDaysOfWeek(m_DaysOfWeek); }
			set { m_DaysOfWeek = DateUtil.MapDaysOfWeek(value); }
		}

		// ---------------------------------

		public TimeBlockSeriesAttributes() { }
		public TimeBlockSeriesAttributes(TimeBlockSeriesAttributes attribs)
		{
			FromDate = attribs.FromDate;
			ToDate = attribs.ToDate;
			FromTime = attribs.FromTime;
			ToTime = attribs.ToTime;
			m_DaysOfWeek = attribs.m_DaysOfWeek;
			SyncToTaskDates = attribs.SyncToTaskDates;
		}

		public string Encode()
		{
			return string.Format("{0}:{1}:{2}:{3}",
								FromTime.TotalMinutes,
								ToTime.TotalMinutes,
								m_DaysOfWeek,
								(SyncToTaskDates ? 1 : 0));
		}

		static public TimeBlockSeriesAttributes Decode(string header)
		{
			var items = header.Split(':');

			if (items.Length != 4)
				return null;

			int fromMins, toMins;
			int daysOfWeek;
			int syncToDates;

			if (!int.TryParse(items[0], out fromMins) ||
				!int.TryParse(items[1], out toMins) ||
				!int.TryParse(items[2], out daysOfWeek) ||
				!int.TryParse(items[3], out syncToDates))
			{
				return null;
			}

			return new TimeBlockSeriesAttributes()
			{
				FromTime = TimeSpan.FromMinutes(fromMins),
				ToTime = TimeSpan.FromMinutes(toMins),
				m_DaysOfWeek = daysOfWeek,
				SyncToTaskDates =(syncToDates != 0)
			};
		}

		public bool IsValid
		{
			get
			{
				return ((FromDate.Date > DateTime.MinValue) &&
						(ToDate.Date < DateTime.MaxValue) &&
						(FromDate.Date <= ToDate.Date) &&
						(FromTime < ToTime) &&
						(m_DaysOfWeek > 0));
			}
		}

		public Calendar.AppointmentDates Dates
		{
			get { return new Calendar.AppointmentDates(FromDate.Date, ToDate.Date); }
			set { FromDate = value.Start.Date; ToDate = value.End.Date; }
		}
	}

	// --------------------------------------------------------------

	public class TimeBlockSeries
	{
		private List<TimeBlock> m_Blocks = new List<TimeBlock>();
		private TimeBlockSeriesAttributes m_Attributes;

		// ----------------------------
		
		public IEnumerable<TimeBlock> Blocks { get { return m_Blocks; } }
		public int BlockCount { get { return m_Blocks.Count; } }

		// ----------------------------

		public TimeBlockSeries()
		{
			m_Attributes = new TimeBlockSeriesAttributes();
		}

		public TimeBlockSeries(TimeBlockSeriesAttributes attribs) : this(attribs, true)
		{
			
		}

		protected TimeBlockSeries(TimeBlockSeriesAttributes attribs, bool addBlocks)
		{
			m_Attributes = new TimeBlockSeriesAttributes(attribs);

			if (addBlocks)
				AddBlocks(attribs.FromDate, attribs.ToDate);
		}

		public TimeBlockSeriesAttributes Attributes
		{
			get
			{
				var dates = Dates;

				return new TimeBlockSeriesAttributes()
				{
					FromDate = Dates.Start,
					ToDate = Dates.End,
					FromTime = m_Attributes.FromTime,
					ToTime = m_Attributes.ToTime,
					DaysOfWeek = DateUtil.MapDaysOfWeek(DateUtil.MapDaysOfWeek(m_Attributes.DaysOfWeek)),
					SyncToTaskDates = m_Attributes.SyncToTaskDates,
				};
			}
		}

		public bool IsValid
		{
			get { return ((m_Attributes != null) && m_Attributes.IsValid && (BlockCount > 0)); }
		}

		public bool SynchroniseDates(TaskItem taskItem)
		{
			if (!m_Attributes.SyncToTaskDates)
				return false;

			// Series should have been deleted if empty
			Debug.Assert(BlockCount > 0);

			bool synced = false;
			var orgDates = Dates;

			// Trim excess blocks outside of the new date range
			if ((taskItem.StartDate.Date > orgDates.Start) || (taskItem.EndDate.Date < orgDates.End))
			{
				int block = BlockCount;

				while (block-- > 0)
				{
					if ((m_Blocks[block].Date < taskItem.StartDate.Date) ||
						(m_Blocks[block].Date > taskItem.EndDate.Date))
					{
						m_Blocks.RemoveAt(block);
						synced = true;
					}
				}
			}

			// Add new blocks at the start of the block range
			synced |= (AddBlocks(taskItem.StartDate, orgDates.Start.AddDays(-1)) > 0);

			// And at the end of the range
			synced |= (AddBlocks(orgDates.End.AddDays(1), taskItem.EndDate) > 0);

			return synced;
		}

		public Calendar.AppointmentDates Dates
		{
			get
			{
				var min = DateTime.MaxValue;
				var max = DateTime.MinValue;

				foreach (var block in m_Blocks)
				{
					if (block.Start < min)
						min = block.Start;

					if (block.End > max)
						max = block.End;
				}

				return new Calendar.AppointmentDates(min.Date, max.Date);
			}
		}

		public bool AddTimeBlock(Calendar.AppointmentDates dates)
		{
			if (dates.Start >= dates.End)
				return false;

			m_Blocks.Add(new TimeBlock(dates));
			return true;
		}

		private int AddBlocks(DateTime from, DateTime to)
		{
			if (from.Date > to.Date)
				return 0;

			var date = from.Date;
			int numAdded = 0;

			do
			{
				var dates = new Calendar.AppointmentDates((date + m_Attributes.FromTime), (date + m_Attributes.ToTime));

				if (m_Attributes.DaysOfWeek.Contains(date.DayOfWeek) && AddTimeBlock(dates))
					numAdded++;

				date = date.AddDays(1);
			}
			while (date.Date <= to.Date);

			return numAdded;
		}

		public bool Contains(TimeBlock block)
		{
			return m_Blocks.Contains(block);
		}

		public bool RemoveTimeBlock(TimeBlock block)
		{
			return m_Blocks.Remove(block);
		}

		public bool DuplicateTimeBlock(TimeBlock block)
		{
			if (!m_Blocks.Contains(block))
				return false;

			m_Blocks.Add(new TimeBlock(block));
			return true;
		}
		
		public string Encode()
		{
			if (BlockCount == 0)
				return string.Empty;

			string timeBlocks = string.Empty;

			foreach (var block in m_Blocks)
				timeBlocks = timeBlocks + block.Encode() + '|';

			return string.Format("{0}/{1}", m_Attributes.Encode(), timeBlocks.TrimEnd('|'));
		}

		static public TimeBlockSeries Create(TimeBlockSeriesAttributes attribs)
		{
			var series = new TimeBlockSeries(attribs, true);

			return series.IsValid ? series : null;
		}

		static public TimeBlockSeries Decode(string blocks)
		{
			if (string.IsNullOrWhiteSpace(blocks))
				return null;

			// Split the header from the blocks
			var items = blocks.Split('/');

			if ((items.Length != 2) ||
				string.IsNullOrWhiteSpace(items[0]) ||
				string.IsNullOrWhiteSpace(items[1]))
			{
				return null;
			}

			var attribs = TimeBlockSeriesAttributes.Decode(items[0]);

			if (attribs == null)
				return null;

			var series = new TimeBlockSeries(attribs, false);
			var pairs = items[1].Split('|');

			foreach (var pair in pairs)
			{
				var dates = TimeBlock.Decode(pair);

				if (dates != null)
					series.m_Blocks.Add(dates);
			}

			if (series.m_Blocks.Count == 0)
				return null;

			return series;
		}
	}

	// ---------------------------------------------------------

	public class TimeBlock : Calendar.AppointmentDates
	{
		public TimeBlock(DateTime start, DateTime end)
		{
			Debug.Assert(start.Date == end.Date);

			Start = start;
			End = end;
		}

		public TimeBlock(Calendar.AppointmentDates dates)
			:
			this(dates.Start, dates.End)
		{
		}

		public DateTime Date { get { return Start.Date; } }

		private const long TicksPerMinute = (60 * 10000000);
		private const long MinutesPerDay = (60 * 24);

		public string Encode()
		{
			long startMin = (Start.Ticks / TicksPerMinute);
			long lenMinutes = (Length.Ticks / TicksPerMinute);

			return string.Format("{0}:{1}", startMin, lenMinutes);
		}

		public static TimeBlock Decode(string block)
		{
			var parts = block.Split(':');

			if (parts.Length != 2)
				return null;

			long startMin = 0, lenMinutes = 0;

			if (!long.TryParse(parts[0], out startMin) || !long.TryParse(parts[1], out lenMinutes))
				return null;

			if ((startMin < 0) || (lenMinutes <= 0))
				return null;

			var startDate = new DateTime(startMin * TicksPerMinute);
			var endDate = new DateTime((startMin + lenMinutes) * TicksPerMinute);

			if (endDate <= startDate)
				return null;

			return new TimeBlock(startDate, endDate);
		}
	}
}
