﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AxeSoftware.Quest.EditorControls
{
    [ControlType("richtext")]
    public partial class RichTextControl : UserControl, IElementEditorControl
    {
        private ControlDataHelper<string> m_helper;
        private IEditorData m_data;

        public RichTextControl()
        {
            InitializeComponent();
            m_helper = new ControlDataHelper<string>(this);
            m_helper.Options.Resizable = true;
            m_helper.Initialise += m_helper_Initialise;

            ctlRichText.Dirty += new WFRichTextControl.DirtyEventHandler(ctlRichText_Dirty);
        }

        void ctlRichText_Dirty(object sender, DataModifiedEventArgs args)
        {
            m_helper.SetDirty((string)ctlRichText.Value);
        }

        void m_helper_Initialise()
        {
            ctlRichText.Initialise(m_helper.Controller, m_helper.ControlDefinition);
        }

        private WFRichTextControl ctlRichText
        {
            get { return (WFRichTextControl)richTextHost.Child; }
        }

        public IControlDataHelper Helper
        {
            get { return m_helper; }
        }

        public void Populate(IEditorData data)
        {
            m_data = data;
            ctlRichText.Populate(data);
        }

        public void Save()
        {
            ctlRichText.Save(m_data);
        }
    }
}