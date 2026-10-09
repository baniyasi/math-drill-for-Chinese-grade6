using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainView : MonoBehaviour
{
    // 运算符枚举
    public enum Operator { Add, Subtract, Multiply, Divide }
    
    // 数字类型
    public enum NumberType { Integer, Fraction, Decimal }
    
    // 算式节点
    public class ExpressionNode
    {
        public bool isNumber; // true为数字，false为运算符
        public object value;  // 数字或运算符
        public bool isCalculated; // 是否已计算
        public int calculationOrder = -1; // 计算顺序
        public bool hasParentheses; // 是否有括号
    }
    
    // 分数结构
    [System.Serializable]
    public struct Fraction
    {
        public int numerator;
        public int denominator;
        
        public Fraction(int num, int den)
        {
            numerator = num;
            denominator = den;
        }
        
        public override string ToString()
        {
            return $"{numerator}/{denominator}";
        }
    }
    
    // 数字结构（可以是整数、分数、小数）
    [System.Serializable]
    public struct Number
    {
        public NumberType type;
        public int integerValue;
        public Fraction fractionValue;
        public float decimalValue;
        
        public override string ToString()
        {
            switch (type)
            {
                case NumberType.Integer: return integerValue.ToString();
                case NumberType.Fraction: return fractionValue.ToString();
                case NumberType.Decimal: return decimalValue.ToString("0.##");
                default: return "0";
            }
        }
    }

        // 配置参数
    [Header("生成配置")]
    public int minNumberCount = 2;
    public int maxNumberCount = 6;
    public float fractionProbability = 0.4f;
    public float decimalProbability = 0.2f;
    
    private List<ExpressionNode> expressionNodes = new List<ExpressionNode>();
    private List<int> calculationOrder = new List<int>();
    private bool hasParentheses;
    private bool isDistributiveLawMode = false;
    private Number commonFactor; // 乘法分配律的公因子
    
    // 生成完整题目
    public void GenerateQuestion()
    {
        expressionNodes.Clear();
        calculationOrder.Clear();
        isDistributiveLawMode = false;
        
        // 1. 生成数字个数
        int numberCount = Random.Range(minNumberCount, maxNumberCount + 1);
        
        // 2. 生成是否有括号
        hasParentheses = Random.Range(0, 2) == 1;
        
        // 3. 生成运算符
        List<Operator> operators = GenerateOperators(numberCount);
        
        // 4. 检查乘法分配律条件
        if (numberCount >= 4 && operators[0] == Operator.Add && operators[1] == Operator.Multiply)
        {
            isDistributiveLawMode = true;
            hasParentheses = false;
            operators[2] = Operator.Add; // 第三个运算符强制为加
        }
        
        // 5. 构建表达式节点列表
        BuildExpressionNodes(numberCount, operators);
        
        // 6. 确定计算顺序
        DetermineCalculationOrder();
        
        // 7. 按照计算顺序生成数字
        GenerateNumbersByCalculationOrder();
        
        // 8. 显示算式
        DisplayExpression();
    }
    
    // 生成运算符列表
    private List<Operator> GenerateOperators(int numberCount)
    {
        List<Operator> operators = new List<Operator>();
        for (int i = 0; i < numberCount - 1; i++)
        {
            operators.Add((Operator)Random.Range(0, 4));
        }
        return operators;
    }

    // 生成随机数字
    private Number GenerateRandomNumber(NumberType preferredType = NumberType.Integer)
    {
        Number number = new Number();
        float rand = Random.value;
        
        if (preferredType != NumberType.Integer)
        {
            number.type = preferredType;
        }
        else if (rand < fractionProbability)
        {
            number.type = NumberType.Fraction;
        }
        else if (rand < fractionProbability + decimalProbability)
        {
            number.type = NumberType.Decimal;
        }
        else
        {
            number.type = NumberType.Integer;
        }
        
        switch (number.type)
        {
            case NumberType.Integer:
                number.integerValue = Random.Range(1, 10);
                break;
                
            case NumberType.Fraction:
                // 生成可约分的分数（合数的分母）
                int[] compositeDenominators = {4, 6, 8, 9, 10, 12, 14, 15, 16, 18, 20};
                int denom = compositeDenominators[Random.Range(0, compositeDenominators.Length)];
                int numer = Random.Range(1, denom);
                number.fractionValue = new Fraction(numer, denom);
                break;
                
            case NumberType.Decimal:
                number.decimalValue = Random.Range(0.1f, 2f);
                // 保留1位小数
                number.decimalValue = Mathf.Round(number.decimalValue * 10) / 10;
                break;
        }
        
        return number;
    }

// 根据已有数字生成匹配的数字（用于通分/约分训练）
    private Number GenerateMatchingNumber(Number existingNumber, Operator op)
    {
        Number newNumber = new Number();
        
        // 尽量使用相同的数字类型
        newNumber.type = existingNumber.type;
        
        switch (existingNumber.type)
        {
            case NumberType.Integer:
                if (op == Operator.Add || op == Operator.Subtract)
                {
                    newNumber.integerValue = Random.Range(1, 10);
                }
                else if (op == Operator.Multiply)
                {
                    // 生成能约分的数字
                    int factor = Random.Range(2, 6);
                    newNumber.integerValue = factor;
                }
                else if (op == Operator.Divide)
                {
                    // 确保能整除或生成分数
                    int divisor = Random.Range(2, 6);
                    newNumber.integerValue = divisor;
                }
                break;
                
            case NumberType.Fraction:
                Fraction existingFraction = existingNumber.fractionValue;
                
                if (op == Operator.Add || op == Operator.Subtract)
                {
                    // 生成需要通分的分数
                    int newDenom = GetDifferentButRelatedDenominator(existingFraction.denominator);
                    int newNumer = Random.Range(1, newDenom);
                    newNumber.fractionValue = new Fraction(newNumer, newDenom);
                }
                else if (op == Operator.Multiply)
                {
                    // 生成能约分的分数
                    int factor = Random.Range(2, 5);
                    newNumber.fractionValue = new Fraction(
                        Random.Range(1, 5) * factor, 
                        Random.Range(2, 6) * factor
                    );
                }
                else if (op == Operator.Divide)
                {
                    // 生成倒数或相关分数
                    newNumber.fractionValue = new Fraction(
                        Random.Range(1, 5), 
                        Random.Range(1, 5)
                    );
                }
                break;
                
            case NumberType.Decimal:
                if (op == Operator.Add || op == Operator.Subtract)
                {
                    newNumber.decimalValue = Random.Range(0.1f, 1f);
                    newNumber.decimalValue = Mathf.Round(newNumber.decimalValue * 10) / 10;
                }
                else
                {
                    newNumber.decimalValue = Random.Range(0.5f, 2f);
                    newNumber.decimalValue = Mathf.Round(newNumber.decimalValue * 10) / 10;
                }
                break;
        }
        
        return newNumber;
    }

    // 获取相关但不相同的分母（用于通分训练）
    private int GetDifferentButRelatedDenominator(int existingDenom)
    {
        // 找有公倍数的分母
        int[] candidates = {2, 3, 4, 6, 8, 12};
        foreach (int candidate in candidates)
        {
            if (candidate != existingDenom && 
                (candidate % existingDenom == 0 || existingDenom % candidate == 0))
            {
                return candidate;
            }
        }
        return existingDenom * 2; // 直接翻倍
    }

    private void GenerateNumbersByCalculationOrder()
    {
        foreach (int opIndex in calculationOrder)
        {
            int leftNumIndex = opIndex - 1;
            int rightNumIndex = opIndex + 1;
            
            Operator currentOp = (Operator)expressionNodes[opIndex].value;
            
            // 获取左右数字
            Number leftNum = (Number)expressionNodes[leftNumIndex].value;
            Number rightNum = (Number)expressionNodes[rightNumIndex].value;
            
            // 检查数字是否已生成
            bool leftGenerated = IsNumberGenerated(leftNum);
            bool rightGenerated = IsNumberGenerated(rightNum);
            
            if (!leftGenerated && !rightGenerated)
            {
                // 两个数字都未生成
                leftNum = GenerateRandomNumber();
                expressionNodes[leftNumIndex].value = leftNum;
                
                rightNum = GenerateMatchingNumber(leftNum, currentOp);
                expressionNodes[rightNumIndex].value = rightNum;
            }
            else if (leftGenerated && !rightGenerated)
            {
                // 只有左边数字已生成
                rightNum = GenerateMatchingNumber(leftNum, currentOp);
                expressionNodes[rightNumIndex].value = rightNum;
            }
            else if (!leftGenerated && rightGenerated)
            {
                // 只有右边数字已生成
                leftNum = GenerateMatchingNumber(rightNum, currentOp);
                expressionNodes[leftNumIndex].value = leftNum;
            }
            
            // 标记为已计算
            expressionNodes[opIndex].isCalculated = true;
        }
    }

    // 检查数字是否已生成（非默认值）
    private bool IsNumberGenerated(Number number)
    {
        switch (number.type)
        {
            case NumberType.Integer: return number.integerValue != 0;
            case NumberType.Fraction: return number.fractionValue.denominator != 0;
            case NumberType.Decimal: return number.decimalValue != 0;
            default: return false;
        }
    }

    // [Header("Prefab引用")]
    // public GameObject numberPrefab;
    // public GameObject operatorPrefab;
    // public GameObject fractionPrefab;
    // public GameObject parenthesesPrefab;
    // public Transform expressionContainer;

    // // 显示算式
    // private void DisplayExpression()
    // {
    //     // 清空容器
    //     foreach (Transform child in expressionContainer)
    //     {
    //         Destroy(child.gameObject);
    //     }
        
    //     for (int i = 0; i < expressionNodes.Count; i++)
    //     {
    //         ExpressionNode node = expressionNodes[i];
    //         GameObject prefabInstance = null;
            
    //         if (node.isNumber)
    //         {
    //             Number number = (Number)node.value;
                
    //             if (number.type == NumberType.Fraction)
    //             {
    //                 prefabInstance = Instantiate(fractionPrefab, expressionContainer);
    //                 // 设置分数显示：需要自定义分数Prefab组件
    //                 FractionDisplay fractionDisplay = prefabInstance.GetComponent<FractionDisplay>();
    //                 if (fractionDisplay != null)
    //                 {
    //                     fractionDisplay.SetFraction(number.fractionValue.numerator, number.fractionValue.denominator);
    //                 }
    //             }
    //             else
    //             {
    //                 prefabInstance = Instantiate(numberPrefab, expressionContainer);
    //                 TMPro.TextMeshProUGUI text = prefabInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>();
    //                 text.text = number.ToString();
    //             }
    //         }
    //         else
    //         {
    //             prefabInstance = Instantiate(operatorPrefab, expressionContainer);
    //             TMPro.TextMeshProUGUI text = prefabInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                
    //             Operator op = (Operator)node.value;
    //             switch (op)
    //             {
    //                 case Operator.Add: text.text = "+"; break;
    //                 case Operator.Subtract: text.text = "-"; break;
    //                 case Operator.Multiply: text.text = "×"; break;
    //                 case Operator.Divide: text.text = "÷"; break;
    //             }
    //         }
            
    //         // 添加括号（简化显示逻辑）
    //         if (node.hasParentheses)
    //         {
    //             // 实际需要更复杂的括号配对逻辑
    //             GameObject leftParen = Instantiate(parenthesesPrefab, expressionContainer);
    //             leftParen.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "(";
    //             // 需要调整插入位置...
    //         }
    //     }
        
        // 自动布局（需要Horizontal Layout Group组件）
        // LayoutRebuilder.ForceRebuildLayoutImmediate(expressionContainer as RectTransform);
    // }












    // Start is called before the first frame update
    void Start()
    {
        GenerateQuestion();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
