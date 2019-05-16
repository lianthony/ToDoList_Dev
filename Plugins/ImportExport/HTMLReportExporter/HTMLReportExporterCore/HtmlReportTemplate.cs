﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;
using System.Net;
using System.Xml;

namespace HTMLReportExporter
{
	class HtmlReportTemplate
	{
		public HtmlReportTemplate()
		{
			HeaderTemplate = "";
			TitleTemplate = "";
			TaskTemplate = "";
			FooterTemplate = "";
		}

		public HtmlReportTemplate(String pathName)
		{
			Load(pathName);
		}

		public String HeaderTemplate { get; set; }
		public String TitleTemplate { get; set; }
		public String TaskTemplate { get; set; }
		public String FooterTemplate { get; set; }

		public String FormatHeaderTemplate()
		{
			// TODO
			return "";
		}
		public String FormatTitleTemplate()
		{
			// TODO
			return "";
		}
		public String FormatTaskTemplate()
		{
			// TODO
			return "";
		}
		public String FormatFooterTemplate()
		{
			// TODO
			return "";
		}
		
		public bool Load(String pathName)
		{
			if (!File.Exists(pathName))
				return false;

			// else
			try
			{
				var doc = XDocument.Load(pathName);

				HeaderTemplate = doc.Root.Element("header").Value;
				TitleTemplate = doc.Root.Element("title").Value;
				TaskTemplate = doc.Root.Element("task").Value;
				FooterTemplate = doc.Root.Element("footer").Value;
			}
			catch
			{
				HeaderTemplate = "";
				TitleTemplate = "";
				TaskTemplate = "";
				FooterTemplate = "";

				return false;
			}

			return true;
		}

		public bool Save(String pathName)
		{
			try
			{
				XDocument doc = new XDocument(new XElement("report",
											new XElement("header", HeaderTemplate),
											new XElement("title", TitleTemplate),
											new XElement("task", TaskTemplate),
											new XElement("footer", FooterTemplate)));
				doc.Save(pathName);
			}
			catch
			{
				return false;
			}

			return true;
		}

	}
}
