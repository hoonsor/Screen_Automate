namespace AutoWizard.Core.Actions.Control
{
    /// <summary>
    /// 單一條件項目，包含條件類型與對應參數
    /// </summary>
    public class ConditionItem
    {
        /// <summary>條件類型</summary>
        public ConditionType ConditionType { get; set; } = ConditionType.VariableEquals;

        /// <summary>左運算元（變數比較/ImageExists 圖片路徑/FileExists 檔案路徑）</summary>
        public string LeftOperand { get; set; } = string.Empty;

        /// <summary>右運算元（變數比較值/ImageExists 門檻）</summary>
        public string RightOperand { get; set; } = string.Empty;

        /// <summary>自由格式條件表達式（Expression 模式）</summary>
        public string ConditionExpression { get; set; } = string.Empty;

        /// <summary>畫面座標 X（ColorMatch 模式）</summary>
        public string ColorXExpression { get; set; } = string.Empty;

        /// <summary>畫面座標 Y（ColorMatch 模式）</summary>
        public string ColorYExpression { get; set; } = string.Empty;

        /// <summary>目標顏色（ColorMatch 模式），如 "#FFFFFF"</summary>
        public string TargetColor { get; set; } = string.Empty;

        /// <summary>色彩容差值 0~255（ColorMatch 模式）</summary>
        public int Tolerance { get; set; } = 0;
    }
}
