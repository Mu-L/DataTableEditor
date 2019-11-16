﻿//------------------------------------------------------------
//
//@Author : Jrimmmmmrz
//
//@Verison : 
//
//@Description : 数据表格处理
//
//------------------------------------------------------------
using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace UnityGameFramework.Editor.DataTableTools
{
    public sealed partial class DataTableProcessor
    {
        private const string CommentLineSeparator = "#";
        private static readonly char[] DataSplitSeparators = new char[] { '\t' };
        private static readonly char[] DataTrimSeparators = new char[] { '\"' };

        private readonly string[] m_NameRow;
        private readonly string[] m_TypeRow;
        private readonly string[] m_DefaultValueRow;
        private readonly string[] m_CommentRow;
        private readonly int m_ContentStartRow;
        private readonly int m_IdColumn;

        private readonly DataProcessor[] m_DataProcessor;
        private readonly string[][] m_RawValues;

        private string m_CodeTemplate;
        private DataTableCodeGenerator m_CodeGenerator;


        /// <summary>
        /// 类型名称的行数
        /// </summary>
        public static int NameRow = 1;

        /// <summary>
        /// 类型的行数
        /// </summary>
        public static int TypeRow = 2;

        /// <summary>
        /// 
        /// </summary>
        public static int DefaultValueRow = -1;

        /// <summary>
        /// 注释行数
        /// </summary>
        public static int CommentRow = 3;

        /// <summary>
        /// 数据表内容开始行数
        /// </summary>
        public static int ContentStartRow = 4;

        /// <summary>
        /// ID开始的列数
        /// </summary>
        public static int IdColumn = 1;

        public int GetIdColumn()
        {
            return IdColumn;
        }



        public DataTableProcessor(string dataTableFileName, Encoding encoding)
        {
            if (string.IsNullOrEmpty(dataTableFileName))
            {
                throw new GameFrameworkException("Data table file name is invalid.");
            }

            if (!dataTableFileName.EndsWith(".txt"))
            {
                throw new GameFrameworkException(Utility.Text.Format("Data table file '{0}' is not a txt.", dataTableFileName));
            }

            if (!File.Exists(dataTableFileName))
            {
                throw new GameFrameworkException(Utility.Text.Format("Data table file '{0}' is not exist.", dataTableFileName));
            }

            string[] lines = File.ReadAllLines(dataTableFileName, encoding);
            int rawRowCount = lines.Length;

            int rawColumnCount = 0;
            List<string[]> rawValues = new List<string[]>();
            for (int i = 0; i < lines.Length; i++)
            {
                string[] rawValue = lines[i].Split(DataSplitSeparators);
                for (int j = 0; j < rawValue.Length; j++)
                {
                    rawValue[j] = rawValue[j].Trim(DataTrimSeparators);
                }

                if (i == 0)
                {
                    rawColumnCount = rawValue.Length;
                }
                else if (rawValue.Length != rawColumnCount)
                {
                    throw new GameFrameworkException(Utility.Text.Format("Raw Column is '{1}', but line '{0}' column is '{2}'.", i.ToString(), rawColumnCount.ToString(), rawValue.Length.ToString()));
                }

                rawValues.Add(rawValue);
            }

            m_RawValues = rawValues.ToArray();

            if (NameRow < 0)
            {
                throw new GameFrameworkException(Utility.Text.Format("Name row '{0}' is invalid.", NameRow.ToString()));
            }

            if (TypeRow < 0)
            {
                throw new GameFrameworkException(Utility.Text.Format("Type row '{0}' is invalid.", TypeRow.ToString()));
            }

            if (ContentStartRow < 0)
            {
                throw new GameFrameworkException(Utility.Text.Format("Content start row '{0}' is invalid.", ContentStartRow.ToString()));
            }

            if (IdColumn < 0)
            {
                throw new GameFrameworkException(Utility.Text.Format("Id column '{0}' is invalid.", IdColumn.ToString()));
            }

            if (NameRow >= rawRowCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Name row '{0}' >= raw row count '{1}' is not allow.", NameRow.ToString(), rawRowCount.ToString()));
            }

            if (TypeRow >= rawRowCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Type row '{0}' >= raw row count '{1}' is not allow.", TypeRow.ToString(), rawRowCount.ToString()));
            }

            if (DefaultValueRow != -1 && DefaultValueRow >= rawRowCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Default value row '{0}' >= raw row count '{1}' is not allow.", DefaultValueRow.ToString(), rawRowCount.ToString()));
            }

            if (CommentRow != -1 && CommentRow >= rawRowCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Comment row '{0}' >= raw row count '{1}' is not allow.", CommentRow.ToString(), rawRowCount.ToString()));
            }

            if (ContentStartRow > rawRowCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Content start row '{0}' > raw row count '{1}' is not allow.", ContentStartRow.ToString(), rawRowCount.ToString()));
            }

            if (IdColumn >= rawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Id column '{0}' >= raw column count '{1}' is not allow.", IdColumn.ToString(), rawColumnCount.ToString()));
            }

            m_NameRow = m_RawValues[NameRow];
            m_TypeRow = m_RawValues[TypeRow];
            m_DefaultValueRow = DefaultValueRow != -1 ? m_RawValues[DefaultValueRow] : null;
            m_CommentRow = CommentRow != -1 ? m_RawValues[CommentRow] : null;

            m_DataProcessor = new DataProcessor[rawColumnCount];
            for (int i = 0; i < rawColumnCount; i++)
            {
                if (i == IdColumn)
                {
                    m_DataProcessor[i] = DataProcessorUtility.GetDataProcessor("id");
                }
                else
                {
                    m_DataProcessor[i] = DataProcessorUtility.GetDataProcessor(m_TypeRow[i]);
                }
            }

            m_CodeTemplate = null;
            m_CodeGenerator = null;
        }

        public int RawRowCount
        {
            get
            {
                return m_RawValues.Length;
            }
        }

        public int RawColumnCount
        {
            get
            {
                return m_RawValues.Length > 0 ? m_RawValues[0].Length : 0;
            }
        }

        public bool IsIdColumn(int rawColumn)
        {
            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            return m_DataProcessor[rawColumn].IsId;
        }

        public bool IsCommentRow(int rawRow)
        {
            if (rawRow < 0 || rawRow >= RawRowCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw row '{0}' is out of range.", rawRow.ToString()));
            }

            return GetValue(rawRow, 0).StartsWith(CommentLineSeparator);
        }

        public bool IsCommentColumn(int rawColumn)
        {
            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            return string.IsNullOrEmpty(GetName(rawColumn)) || m_DataProcessor[rawColumn].IsComment;
        }

        public string GetName(int rawColumn)
        {
            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            if (IsIdColumn(rawColumn))
            {
                return "Id";
            }

            return m_NameRow[rawColumn];
        }

        public bool IsSystem(int rawColumn)
        {
            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            return m_DataProcessor[rawColumn].IsSystem;
        }

        public System.Type GetType(int rawColumn)
        {
            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            return m_DataProcessor[rawColumn].Type;
        }

        public string GetLanguageKeyword(int rawColumn)
        {
            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            return m_DataProcessor[rawColumn].LanguageKeyword;
        }

        public string GetDefaultValue(int rawColumn)
        {
            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            return m_DefaultValueRow != null ? m_DefaultValueRow[rawColumn] : null;
        }

        public string GetComment(int rawColumn)
        {
            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            return m_CommentRow != null ? m_CommentRow[rawColumn] : null;
        }

        public string GetValue(int rawRow, int rawColumn)
        {
            if (rawRow < 0 || rawRow >= RawRowCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw row '{0}' is out of range.", rawRow.ToString()));
            }

            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            return m_RawValues[rawRow][rawColumn];
        }

        public bool GenerateDataFile(string outputFileName, Encoding encoding)
        {
            if (string.IsNullOrEmpty(outputFileName))
            {
                throw new GameFrameworkException("Output file name is invalid.");
            }

            try
            {
                using (FileStream fileStream = new FileStream(outputFileName, FileMode.Create))
                {
                    using (BinaryWriter stream = new BinaryWriter(fileStream, encoding))
                    {
                        for (int i = ContentStartRow; i < RawRowCount; i++)
                        {
                            if (IsCommentRow(i))
                            {
                                continue;
                            }

                            int startPosition = (int)stream.BaseStream.Position;
                            stream.BaseStream.Position += sizeof(int);
                            for (int j = 0; j < RawColumnCount; j++)
                            {
                                if (IsCommentColumn(j))
                                {
                                    continue;
                                }

                                try
                                {
                                    m_DataProcessor[j].WriteToStream(stream, GetValue(i, j));
                                }
                                catch
                                {
                                    if (m_DataProcessor[j].IsId || string.IsNullOrEmpty(GetDefaultValue(j)))
                                    {
                                        Debug.LogError(Utility.Text.Format("Parse raw value failure. OutputFileName='{0}' RawRow='{1}' RowColumn='{2}' Name='{3}' Type='{4}' RawValue='{5}'", outputFileName, i.ToString(), j.ToString(), GetName(j), GetLanguageKeyword(j), GetValue(i, j)));
                                        return false;
                                    }
                                    else
                                    {
                                        Debug.LogWarning(Utility.Text.Format("Parse raw value failure, will try default value. OutputFileName='{0}' RawRow='{1}' RowColumn='{2}' Name='{3}' Type='{4}' RawValue='{5}'", outputFileName, i.ToString(), j.ToString(), GetName(j), GetLanguageKeyword(j), GetValue(i, j)));
                                        try
                                        {
                                            m_DataProcessor[j].WriteToStream(stream, GetDefaultValue(j));
                                        }
                                        catch
                                        {
                                            Debug.LogError(Utility.Text.Format("Parse default value failure. OutputFileName='{0}' RawRow='{1}' RowColumn='{2}' Name='{3}' Type='{4}' RawValue='{5}'", outputFileName, i.ToString(), j.ToString(), GetName(j), GetLanguageKeyword(j), GetComment(j)));
                                            return false;
                                        }
                                    }
                                }
                            }

                            int endPosition = (int)stream.BaseStream.Position;
                            int length = endPosition - startPosition - sizeof(int);
                            stream.BaseStream.Position = startPosition;
                            stream.Write(length);
                            stream.BaseStream.Position = endPosition;
                        }
                    }
                }

                Debug.Log(Utility.Text.Format("Parse data table '{0}' success.", outputFileName));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(Utility.Text.Format("Parse data table '{0}' failure, exception is '{1}'.", outputFileName, exception.ToString()));
                return false;
            }
        }

        public bool SetCodeTemplate(string codeTemplateFileName, Encoding encoding)
        {
            try
            {
                m_CodeTemplate = File.ReadAllText(codeTemplateFileName, encoding);
                Debug.Log(Utility.Text.Format("Set code template '{0}' success.", codeTemplateFileName));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(Utility.Text.Format("Set code template '{0}' failure, exception is '{1}'.", codeTemplateFileName, exception.ToString()));
                return false;
            }
        }

        public void SetCodeGenerator(DataTableCodeGenerator codeGenerator)
        {
            m_CodeGenerator = codeGenerator;
        }

        public bool GenerateCodeFile(string outputFileName, Encoding encoding, object userData = null)
        {
            if (string.IsNullOrEmpty(m_CodeTemplate))
            {
                throw new GameFrameworkException("You must set code template first.");
            }

            if (string.IsNullOrEmpty(outputFileName))
            {
                throw new GameFrameworkException("Output file name is invalid.");
            }

            try
            {
                StringBuilder stringBuilder = new StringBuilder(m_CodeTemplate);
                if (m_CodeGenerator != null)
                {
                    m_CodeGenerator(this, stringBuilder, userData);
                }

                using (FileStream fileStream = new FileStream(outputFileName, FileMode.Create))
                {
                    using (StreamWriter stream = new StreamWriter(fileStream, encoding))
                    {
                        stream.Write(stringBuilder.ToString());
                    }
                }

                Debug.Log(Utility.Text.Format("Generate code file '{0}' success.", outputFileName));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(Utility.Text.Format("Generate code file '{0}' failure, exception is '{1}'.", outputFileName, exception.ToString()));
                return false;
            }
        }
    }
}