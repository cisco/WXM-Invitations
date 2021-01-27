using OfficeOpenXml;
using OfficeOpenXml.Table.PivotTable;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;

namespace DPReporting
{
    public static class PivotFilterExtension
    {
        //changes the XML data of excel file to include certain calculations in the pivot since its not directly possible through epplus
        public static void ModifyXMLForPivot(this ExcelPivotTable pivot, ExcelPivotTableDataField f)
        {
            var xdoc = pivot.PivotTableXml;
            var nsm = new XmlNamespaceManager(xdoc.NameTable);

            // "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
            var schemaMain = xdoc.DocumentElement.NamespaceURI;
            if (nsm.HasNamespace("x") == false)
                nsm.AddNamespace("x", schemaMain);

            var schemaMainX14 = "http://schemas.microsoft.com/office/spreadsheetml/2009/9/main";
            if (nsm.HasNamespace("x14") == false)
                nsm.AddNamespace("x14", schemaMainX14);

            // <x:pivotTableDefinition updatedVersion="5">
            var pivotTableDefinition = xdoc.SelectSingleNode("/x:pivotTableDefinition", nsm);
            XmlAttribute newAttr = xdoc.CreateAttribute("updatedVersion");
            newAttr.Value = "5";
            pivotTableDefinition.Attributes.Append(newAttr); //("showDataAs", "percentOfTotal");

            //pivotTableDefinition.AppendAttribute("updatedVersion", "5");

            // <x:dataField name="% Parent">
            var dataFieldNode = xdoc.SelectSingleNode(
                "/x:pivotTableDefinition/x:dataFields/x:dataField[@name='" + f.Name + "']",
                nsm
            );

            XmlElement child1 = xdoc.CreateElement("x:extLst", schemaMain);
            XmlElement child2 = xdoc.CreateElement("x:ext", schemaMain);
            XmlElement child3 = xdoc.CreateElement("x14:dataField", schemaMainX14);

            // <x:extLst>
            var extLst = dataFieldNode.AppendChild(child1);

            // <x:ext uri="{E15A36E0-9728-4e99-A89B-3F7291B0FE68}">
            var ext = extLst.AppendChild(child2);
            newAttr = xdoc.CreateAttribute("uri");
            newAttr.Value = "{E15A36E0-9728-4e99-A89B-3F7291B0FE68}";
            ext.Attributes.Append(newAttr);

            // <x14:dataField pivotShowAs="percentOfParentRow">
            var x14DataField = ext.AppendChild(child3);
            newAttr = xdoc.CreateAttribute("pivotShowAs");
            newAttr.Value = "percentOfParentRow";
            x14DataField.Attributes.Append(newAttr);
        }

        //will configure the default split by pivot table
        public static bool ConfigurePivot(this ExcelPivotTable pivot, string FilterFieldName, string PrimaryRowField, string RowCaption, bool skipPrimaryRow = false, bool includeCompletionStatus = true, string grandTotalCaption = "Total Sent", string SecondaryRowField = null)
        {
            if ((FilterFieldName == null || PrimaryRowField == null || RowCaption == null) && !skipPrimaryRow)
                return false;

            var modelField = pivot.Fields[FilterFieldName];
            pivot.PageFields.Add(modelField);

            if (!skipPrimaryRow)
            {
                pivot.RowFields.Add(pivot.Fields[PrimaryRowField]);
                pivot.DataOnRows = false;

                pivot.RowHeaderCaption = RowCaption;

                if (!string.IsNullOrEmpty(SecondaryRowField))
                {
                    pivot.RowFields.Add(pivot.Fields[SecondaryRowField]);
                    pivot.DataOnRows = false;
                }
            }
            else
                pivot.RowHeaderCaption = "Overall";

            pivot.RowFields.Add(pivot.Fields["Response Status"]);
            pivot.DataOnRows = false;
            if (!skipPrimaryRow)
                pivot.RowFields[1].Sort = eSortType.Ascending;
            else
                pivot.RowFields[0].Sort = eSortType.Ascending;

            if (includeCompletionStatus)
            {
                pivot.RowFields.Add(pivot.Fields["Completion Status"]);
                pivot.DataOnRows = false;
            }
            
            //pivot.RowHeaderCaption = RowCaption;

            pivot.GrandTotalCaption = grandTotalCaption;

            pivot.ColumnHeaderCaption = "Response Status";

            var field = pivot.DataFields.Add(pivot.Fields["Response Status"]);
            field.Name = "Messages Sent by " + RowCaption;
            field.Function = DataFieldFunctions.Count;
            field.Format = "0";

            field = pivot.DataFields.Add(pivot.Fields["Response Status"]);
            field.Name = "% Messages Sent by " + RowCaption;
            field.Function = DataFieldFunctions.Count;
            field.Format = "0.00%";

            pivot.ModifyXMLForPivot(field);

            return true;
        }

    }
}
