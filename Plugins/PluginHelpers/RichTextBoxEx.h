﻿#pragma once

////////////////////////////////////////////////////////////////////////////////////////////////

using namespace System;

////////////////////////////////////////////////////////////////////////////////////////////////

namespace Abstractspoon
{
	namespace Tdl
	{
		namespace PluginHelpers
		{
			public ref class RichTextBoxEx : Windows::Forms::RichTextBox
			{
			public:
				bool SelectionContainsPos(Drawing::Point ptClient);
				bool SelectionContainsMessagePos();

			protected:
				bool m_ShowHandCursor = false;
				String^ m_ContextUrl;

			protected:
				virtual property Windows::Forms::CreateParams^ CreateParams
				{
					Windows::Forms::CreateParams^ get() override;
				}

				virtual void WndProc(Windows::Forms::Message% m) override;

				String^ GetTextRange(const CHARRANGE& cr);

				HWND HWnd();
			};
		}
	}
}
