using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ModernityAnalyzer;
//TODO's:
//Check the method of counting features for duplicates.
public class SolutionAnalyzer
{

    private Dictionary<double, Dictionary<string, int>>? Results { get; }
    private Dictionary<DateTime, Dictionary<double, Dictionary<string, int>>>? ResultsByTime { get; }
    public SolutionAnalyzer()
    {
        Results = new Dictionary<double, Dictionary<string, int>>();
    }
    public CommitResults AnalyzeRepo(Solution solution, string repositoryPath, string date)
    {
        TextWriter backupOut = Console.Out;
       
        try
        {

            var csFiles = solution.Projects
                .SelectMany(project => project.Documents)
                .Where(doc => doc.FilePath.EndsWith(".cs") &&
                !doc.FilePath.Contains(@"\.git\"))
                .Select(doc => doc.FilePath);

            if (!csFiles.Any())
            {
                Console.WriteLine($"No C# files found in the solution at {repositoryPath}.");
                return null;
            }

            var commitCard = CommitFactory.BuildCommitCard(date);
            foreach (var file in csFiles)
            {

                Console.SetOut(TextWriter.Null);
                if (!File.Exists(file))
                {
                    Console.WriteLine($"!!!--File {file} does not exist.--!!!");
                    continue;
                }
                string code = File.ReadAllText(file);

                AnalyzeFile(commitCard, code);
                Console.SetOut(backupOut);
                //TODO: Call analyze method here to analyze the code and get the features used.

            }

            return commitCard;
        }
        catch (Exception e)
        {
            Console.SetOut(backupOut);
            Console.WriteLine(e.Message);
            Console.WriteLine("-----------------------------------------------------------------------------");
            Console.WriteLine(e.StackTrace);
        }
        return null;
    }

    private static void AnalyzeFile(CommitResults commitCard, string code)
    {
        //Console.SetOut(TextWriter.Null);

        var AST = CSharpSyntaxTree.ParseText(code);

        var root = AST.GetCompilationUnitRoot();

        //Try to get language version from the file
        CSharpParseOptions parseOptions = AST.Options as CSharpParseOptions;
        LanguageVersion languageVersion = parseOptions?.LanguageVersion ?? LanguageVersion.Default;

        var compilation = CSharpCompilation.Create("Analysis",
            new[] { AST },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication)
            );

        var semanticModel = compilation.GetSemanticModel(AST);
        if (semanticModel == null)
        {
            Console.WriteLine("Semantic model could not be created. Skipping analysis.");
            return;
        }
        TraverseAST(root, commitCard, semanticModel, "");
    }

    static void TraverseAST(SyntaxNode node, CommitResults card, SemanticModel semanticModel, string indent)
    {
        try
        {
            //Console.WriteLine($"{indent}Node kind: {node.Kind()}");

            //--------------------7.1--------------------
            if (node == null || semanticModel == null) return;

            IsPatternExpression(node, card);

            InferedTupleElementNames(node, card);

            IsDefaultExpression(node, card);

            IsAsyncMain(node, card);

            //--------------------7.2--------------------
            IsPrivateProtected(node, card);

            IsNonTrailingNamedArguments(node, card);

            IsDigitSeparator(node, card);

            IsConditionalRefExpression(node, card);

            IsInParameters(node, card);

            IsInArguments(node, card);

            IsRefOrReadonlyStruct(node, card);

            //--------------------7.3--------------------

            IsUnmanagedTypeConstraint(node, card);

            IsTupleEquality(node, card, semanticModel);

            IsAutoImplementedPropertyFieldTargetedAttributes(node, card);

            IsStackallocArrayInitializers(node, card);

            IsFixedSizedBuffers(node, card);

            IsExpressionVariablesInInitializers(node, card);

            //--------------------8.0--------------------

            IsReadonlyInstanceMembers(node, card);

            IsStaticLocalFunctions(node, card);

            IsStackallocInNestedContexts(node, card);

            IsAlternativeInterpolatedVervatimStrings(node, card);

            IsSystemRangeOrSystemIndex(node, card);

            IsIndexType(node, card);

            IsRangeType(node, card);

            IsPatternBasedUsing(node, card);

            IsRecursivePatternMatching(node, card);

            IsDefaultInterfaceMethod(node, card);

            //--------------------9.0--------------------

            IsTargetTypedNewExpression(node, card);
            IsSkipLocalsInitAttribute(node, card);
            IsLambdaDiscardPatameters(node, card);
            IsNativeSizedIntegers(node, card);
            IsAttributeOnLocalFunction(node, card);
            IsFunctionPointer(node, card);
            IsStaticLambda(node, card);
            IsRecordType(node, card);
            IsTargetedTypeExpressions(node, card);
            IsCovariantReturn(node, card, semanticModel);
            IsGetEnumeratorRecognitionInForeach(node, card, semanticModel);
            ISModuleInitializers(node, card);
            IsTopLevelStatement(node, card);

            //---------------------10.0--------------------

            IsRecordStructDeclaration(node, card);
            IsGlobalUsing(node, card);
            IsConstantInterpolatedStrings(node, card);
            IsRecordsWithSealedBaseToStringOverride(node, card);
            IsMixOfDeclarationsAndVariablesInTupleDeconstruction(node, card);
            IsAllowAsyncMethodBuilderDecorator(node, card);
            IsStaticAbstractMembersInInterfaces(node, card);
            IsStackallocArrayInitializers(node, card);
            IsLambdaImprovement(node, card);
            IsParameterlessStructConstructor(node, card);
            IsCallerExpressionAttribute(node, card);

            //----------------------11.0--------------------
            IsFileLocalTypes(node, card);
            IsRequiredMembers(node, card);
            IsUnsignedRightShift(node, card);
            IsUtf8StringLiterals(node, card);
            IsPatternMatchingOnReadOnlySpanOfChars(node, card, semanticModel);
            IsCheckedOperator(node, card);
            IsAutoDefaultStruct(node, card);
            IsListPattern(node, card);
            IsRawStringLiteral(node, card);
            IsNameOf(node, card);
            IsGenericAttribute(node, card);

            //----------------------12.0--------------------

            IsRefReadonlyParameter(node, card);
            IsCollectionExpression(node, card);
            IsInlineArrays(node, card);
            IsNameofAccessingInstanceMembers(node, card);
            IsUsingAliases(node, card);
            IsPrimaryConstructor(node, card);
            IsLambdaOptionalParameters(node, card);

        }catch(Exception e)
        {
            Console.WriteLine($"{e.Message}");
            Console.WriteLine($"{e.StackTrace}");
        }
        foreach (var child in node.ChildNodes())
        {
            TraverseAST(child, card, semanticModel, indent + "   ");
        }
        ;
    }

    //--------------------7.1--------------------
    static void IsPatternExpression(SyntaxNode node, CommitResults card)
    {
        if (node is IsPatternExpressionSyntax isPatternExpression)
        {
            Console.WriteLine("Found pattern matching in generic method --> 7.1");
            // data.dataStruct[7.1] += 1;
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.1);
            entry.Values["Pattern matching with generics"] += 1;
        }

    }

    static void InferedTupleElementNames(SyntaxNode node, CommitResults card)
    {
        if (node.IsKind(SyntaxKind.SimpleAssignmentExpression))
        {
            var assignmentExpression = (AssignmentExpressionSyntax)node;

            // Check if the right-hand side of the assignment is a tuple deconstruction
            if (assignmentExpression.Right.IsKind(SyntaxKind.TupleExpression))
            {
                var tupleExpression = (TupleExpressionSyntax)assignmentExpression.Right;

                // Check if tuple elements have explicitly specified names
                if (tupleExpression.Arguments.All(argument => argument.NameColon == null))
                {
                    Console.WriteLine($"Inferred tuple element names detected --> 7.1");
                    var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.1);
                    entry.Values["Inffered tuple element names"] += 1;
                }
            }
        }
    }

    static void IsDefaultExpression(SyntaxNode node, CommitResults card)
    {
        if (node is DefaultExpressionSyntax)
        {
            Console.WriteLine("Found default expression --> 7.1");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.1);
            entry.Values["Default expressions"] += 1;
        }
        else if (node is LiteralExpressionSyntax literalExpression1 && literalExpression1.ToString().Equals("default"))
        {
            Console.WriteLine("Found default expression --> 7.1");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.1);
            entry.Values["Default expressions"] += 1;
        }
    }

    static void IsAsyncMain(SyntaxNode node, CommitResults card)
    {
        if (node is MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration.ReturnType.ToString().Contains("Task"))
            {
                Console.WriteLine("Found Task as return --> 7.1");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.1);
                entry.Values["async Main"] += 1;
            }
        }
    }

    //--------------------7.2--------------------
    static void IsPrivateProtected(SyntaxNode node, CommitResults card)
    {
        if (node is MemberDeclarationSyntax memberDeclaration)
        {
            // Check if the modifiers contain both "private" and "protected"
            bool hasPrivate = false;
            bool hasProtected = false;

            foreach (var modifier in memberDeclaration.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.PrivateKeyword)) hasPrivate = true;
                if (modifier.IsKind(SyntaxKind.ProtectedKeyword)) hasProtected = true;
            }

            if (hasPrivate && hasProtected)
            {
                // Do SMTH
                Console.WriteLine("Field with 'private protected' modifier --> 7.2");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.2);
                entry.Values["private protected combination"] += 1;
            }
        }
    }

    static void IsNonTrailingNamedArguments(SyntaxNode node, CommitResults card)
    {
        if (node is InvocationExpressionSyntax invocation)
        {
            if (CheckArguments(invocation.ArgumentList.Arguments))
            {
                Console.WriteLine("Non - trailing named arguments found --> 7.2");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.2);
                entry.Values["Non trailing arguments"] += 1;
            }
        }
    }

    static void IsDigitSeparator(SyntaxNode node, CommitResults card)
    {
        if (node is LiteralExpressionSyntax literalExpression)
        {
            // Get the text representation of the literal
            string literalText = literalExpression.Token.Text;

            // Check if contains an underscore after 0x or 0b
            if (literalText.Contains("0b_") || literalText.Contains("0x_"))
            {
                Console.WriteLine("Found digit separator after 0b or 0x --> 7.2");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.2);
                entry.Values["Digit separator after 0b or 0x"] += 1;
            }
        }
    }

    static void IsConditionalRefExpression(SyntaxNode node, CommitResults card)
    {
        if (node is ConditionalExpressionSyntax conditionalExpression)
        {
            // Check if the conditional expression is used as a ref
            if (IsRefConditionalExpression(conditionalExpression))
            {
                Console.WriteLine("Found conditional ref expression --> 7.2");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.2);
                entry.Values["conditional ref"] += 1;
            }
        }
    }

    static void IsInParameters(SyntaxNode node, CommitResults card)
    {
        if (node is ParameterSyntax parameterExpression)
        {
            // Check if the parameter has the 'in' modifier
            if (parameterExpression.Modifiers.Any(SyntaxKind.InKeyword))
            {
                Console.WriteLine("Found IN keyword in parameter --> 7.2");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.2);
                entry.Values["IN parameters"] += 1;
            }
        }
    }

    static void IsInArguments(SyntaxNode node, CommitResults card)
    {
        if (node is ArgumentSyntax argumentExpr)
        {
            // Check if the argument has the 'in' modifier
            if (argumentExpr.RefKindKeyword.IsKind(SyntaxKind.InKeyword))
            {
                Console.WriteLine("Found IN keyword in argument --> 7.2");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.2);
                entry.Values["IN arguments"] += 1;
            }
        }
    }

    static void IsRefOrReadonlyStruct(SyntaxNode node, CommitResults card)
    {
        var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.2);

        if (node is StructDeclarationSyntax structDeclaration5)
        {
            // Check for ref struct
            if (structDeclaration5.Modifiers.Any(SyntaxKind.RefKeyword))
            {
                Console.WriteLine($"ref struct detected --> 7.2");
                entry.Values["ref struct"] += 1;
            }

            // Check for readonly struct
            if (structDeclaration5.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
            {
                Console.WriteLine($"readonly struct detected --> 7.2");
                entry.Values["reaonly struct"] += 1;
            }
        }
        else if (node is ParameterSyntax parameter)
        {
            // Check for in parameter
            if (parameter.Modifiers.Any(SyntaxKind.InKeyword))
            {
                Console.WriteLine($"in parameter detected --> 7.2");
                entry.Values["in"] += 1;
            }
        }
        else if (node is VariableDeclarationSyntax variableDeclaration1)
        {
            // Check for Span<T> or ReadOnlySpan<T>
            var type = variableDeclaration1.Type;
            if (type is GenericNameSyntax genericName)
            {
                if (genericName.Identifier.Text == "Span" || genericName.Identifier.Text == "ReadOnlySpan")
                {
                    Console.WriteLine($"{genericName.Identifier.Text} detected --> 7.2");
                    entry.Values["span<T> and readonlySpan"] += 1;
                }
            }
        }
    }

    //--------------------7.3--------------------

    static void IsUnmanagedTypeConstraint(SyntaxNode node, CommitResults card)
    {
        if (node is TypeParameterConstraintClauseSyntax constraintClause)
        {
            foreach (var constraint in constraintClause.Constraints)
            {
                if (constraint is TypeConstraintSyntax typeConstraint &&
                    typeConstraint.Type is IdentifierNameSyntax identifier &&
                    identifier.Identifier.Text == "unmanaged")
                {
                    Console.WriteLine("Found unmanaged constraint --> 7.3");
                    var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.3);
                    entry.Values["Unmanaged type constraint"] += 1;
                }
            }
        }
    }

    static void IsTupleEquality(SyntaxNode node, CommitResults card, SemanticModel model)
    {
        if (node is BinaryExpressionSyntax binaryExpression)
        {
            // Check if the binary expression uses '==' or '!=' operators
            if (node.IsKind(SyntaxKind.EqualsExpression) || node.IsKind(SyntaxKind.NotEqualsExpression))
            {
                //var test = model.GetTypeInfo(binaryExpression.Left);
                //Console.WriteLine($"{test.Type.IsTupleType}");
                //Console.WriteLine($"{test.Type.Name is Tuple}");

                // Check if both sides of the binary expression are tuples with semantic model
                try
                {
                    if (model != null && binaryExpression.Left != null && binaryExpression.Right != null && model.GetTypeInfo(binaryExpression.Left).Type.IsTupleType && model.GetTypeInfo(binaryExpression.Right).Type.IsTupleType)
                    {
                        Console.WriteLine($"Found != or == for tuples --> 7.3");
                        var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.3);
                        entry.Values["Support == and != for tuples"] += 1;
                    }
                }
                catch (Exception ex) { }
            }
        }
    }

    static void IsAutoImplementedPropertyFieldTargetedAttributes(SyntaxNode node, CommitResults card)
    {
        if (node is PropertyDeclarationSyntax propertyDeclaration)
        {
            // Check the attributes of the property
            foreach (var attributeList in propertyDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    // Remove attribute from attribute list and see if it contains field
                    if (attributeList.ToString().Replace(attribute.ToString(), "").Contains("field"))
                    {
                        Console.WriteLine($"Found field-targeted attribute --> 7.3");
                        var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.3);
                        entry.Values["C# 7.3: Auto-Implemented Property Field-Targeted Attributes"] += 1;
                    }
                }
            }
        }
    }

    static void IsStackallocArrayInitializers(SyntaxNode node, CommitResults card)
    {
        // C# 7.3: Stackalloc array initializers
        if (node is StackAllocArrayCreationExpressionSyntax stackAllocExpression)
        {
            // Check if the stackalloc expression has an initializer
            if (stackAllocExpression.Initializer != null)
            {
                Console.WriteLine("Found stackalloc array initializers --> 7.3");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.3);
                entry.Values["Stackalloc array initializers"] += 1;
            }
        }
    }
    static void IsFixedSizedBuffers(SyntaxNode node, CommitResults card)
    {
        var newFixedBufferTypes = new[] { "bool", "char", "short", "ushort" };

        if (node is StructDeclarationSyntax structDeclaration)
        {
            // Traverse the members of the struct
            foreach (var member in structDeclaration.Members)
            {
                // Check if the member is a field declaration with fixed-size buffer
                if (member is FieldDeclarationSyntax fieldDeclaration)
                {
                    // Get the type of the fixed-size buffer
                    var variableType = fieldDeclaration.Declaration.Type;

                    //Console.WriteLine($"TEST: {fieldDeclaration.Declaration.Type}");
                    if (variableType != null)
                    {
                        if (newFixedBufferTypes.Contains(variableType.ToString()))
                        {
                            Console.WriteLine($"Found fixed-size buffer --> 7.3");
                            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.3);
                            entry.Values["fixed-sized buffers"] += 1;
                        }
                    }
                }
            }
        }
    }

    static void IsExpressionVariablesInInitializers(SyntaxNode node, CommitResults card)
    {
        if (node is InitializerExpressionSyntax initializer && initializer.Kind().ToString().Equals("ObjectInitializerExpression"))
        {
            // Traverse the expressions within the initializer
            foreach (var expression in initializer.Expressions)
            {
                //Console.WriteLine($"{node.Kind()}");
                //Console.WriteLine($"{node.GetType()}");
                // Check if the 2nd part of expression is not a simple assignment or variable reference
                try
                {
                    if (!(expression.ChildNodes().Last() is IdentifierNameSyntax) && !(expression.ChildNodes().Last() is AssignmentExpressionSyntax))
                    {
                        Console.WriteLine("Found expression in object initializer --> 7.3");
                        var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 7.3);
                        entry.Values["Expression variables in initializers"] += 1;
                    }
                }
                catch (Exception _) { }
            }
        }
    }

    //--------------------8.0--------------------

    static void IsReadonlyInstanceMembers(SyntaxNode node, CommitResults card)
    {
        var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 8.0);

        if (node is MethodDeclarationSyntax method && method.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
        {
            Console.WriteLine($"Readonly method found --> 8.0");
            entry.Values["Readonly Method"] += 1;
        }
        else if (node is PropertyDeclarationSyntax property && property.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
        {
            Console.WriteLine($"Readonly property found --> 8.0");
            entry.Values["Readonly Property"] += 1;
        }
    }

    static void IsStaticLocalFunctions(SyntaxNode node, CommitResults card)
    {
        if (node is LocalFunctionStatementSyntax localFunction && localFunction.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            Console.WriteLine($"Static local function found --> 8.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 8.0);
            entry.Values["Static local functions"] += 1;
        }
    }

    static void IsStackallocInNestedContexts(SyntaxNode node, CommitResults card)
    {
        if (node is StackAllocArrayCreationExpressionSyntax stackAlloc)
        {
            var parent = stackAlloc.Parent;
            while (parent != null && !(parent is BlockSyntax))
            {
                // If stackalloc is found within an expression context, report it
                if (parent is ExpressionSyntax)
                {
                    var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 8.0);
                    entry.Values["Stackalloc in nested contexts"] += 1;
                    break;
                }
                parent = parent.Parent;
            }
        }
    }

    static void IsAlternativeInterpolatedVervatimStrings(SyntaxNode node, CommitResults card)
    {
        if (node is InterpolatedStringExpressionSyntax interpolatedString)
        {
            var check = interpolatedString.GetFirstToken();
            if (check.Text.StartsWith("@$") || check.Text.StartsWith("$@"))
            {
                Console.WriteLine($"$@ or @$ string declaration found --> 8.0");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 8.0);
                entry.Values["Alternative interpolated verbatim strings"] += 1;
            }
        }
    }

    static void IsSystemRangeOrSystemIndex(SyntaxNode node, CommitResults card)
    {
        if (node is RangeExpressionSyntax rangeExpression)
        {
            Console.WriteLine($"Detected range expression --> 8.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 8.0);
            entry.Values["Ranges: System.Index and System.Range, and ^"] += 1;
        }
    }
    static void IsIndexType(SyntaxNode node, CommitResults card)
    {
        if (node is IdentifierNameSyntax indexType && indexType.Identifier.Text == "Index")
        {
            Console.WriteLine($"Detected Index type --> 8.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 8.0);
            entry.Values["IndexType"] += 1;
        }
    }
    static void IsRangeType(SyntaxNode node, CommitResults card)
    {
        if (node is IdentifierNameSyntax rangeType && rangeType.Identifier.Text == "Range")
        {
            Console.WriteLine($"Detected Range type --> 8.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 8.0);
            entry.Values["RangeType"] += 1;
        }
    }
    static void IsPatternBasedUsing(SyntaxNode node, CommitResults card)
    {
        if (node is UsingStatementSyntax usingStatement)
        {
            Console.WriteLine($"Detected pattern-based using --> 8.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 8.0);
            entry.Values["Enhanced using"] += 1;
        }
        if (node is LocalDeclarationStatementSyntax localDeclaration && localDeclaration.UsingKeyword.ToString().Equals("using"))
        {
            Console.WriteLine($"Detected using declaration --> 8.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 8.0);
            entry.Values["Enhanced using"] += 1;
        }
    }
    static void IsRecursivePatternMatching(SyntaxNode node, CommitResults card)
    {
        if (node is SwitchStatementSyntax switchStatement)
        {
            // Check for case patterns with nested patterns
            var casePatterns = switchStatement.DescendantNodes().OfType<CasePatternSwitchLabelSyntax>();
            foreach (var casePattern in casePatterns)
            {
                if (casePattern.Pattern.DescendantNodes().OfType<DeclarationPatternSyntax>().Any())
                {
                    Console.WriteLine("Recursive pattern matching detected in switch statement --> 8.0");
                    var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 8.0);
                    entry.Values["Recursive Pattern Matching"] += 1;
                    return;
                }
            }
        }
    }
    static void IsDefaultInterfaceMethod(SyntaxNode node, CommitResults card)
    {
        if (node is InterfaceDeclarationSyntax interfaceDeclaration)
        {
            foreach (var member in interfaceDeclaration.Members.OfType<MethodDeclarationSyntax>())
            {
                if (member.Body != null)
                {
                    Console.WriteLine($"Default interface method detected --> 8.0");
                    var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 8.0);
                    entry.Values["default interface methods"] += 1;
                }
            }
        }
    }

    //--------------------9.0--------------------

    static void IsTargetTypedNewExpression(SyntaxNode node, CommitResults card)
    {
        if (node is ImplicitObjectCreationExpressionSyntax implicitNew)
        {
            Console.WriteLine("Target-typed new expression detected --> 9.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 9.0);
            entry.Values["Target-typed new expression"] += 1;
        }
    }
    static void IsSkipLocalsInitAttribute(SyntaxNode node, CommitResults card)
    {
        if (node is AttributeSyntax attributeArgument && attributeArgument.ToString().Equals("SkipLocalsInit"))
        {
            //Console.WriteLine($"{attributeArgument}");
            Console.WriteLine("SkipLocalsInit detected  --> 9.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 9.0);
            entry.Values["SkipLocalsInit Attribute"] += 1;
        }
    }
    static void IsLambdaDiscardPatameters(SyntaxNode node, CommitResults card)
    {
        if (node is SimpleLambdaExpressionSyntax simpleLambda)
        {
            // Check if the parameter is a discard
            if (simpleLambda.Parameter.Identifier.Text == "_")
            {
                Console.WriteLine("Discard parameter detected in simple lambda expression --> 9.0");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 9.0);
                entry.Values["Lambda discard parameters"] += 1;
            }
        }
        else if (node is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
        {
            // Check if any of the parameters are discards
            foreach (var parameter in parenthesizedLambda.ParameterList.Parameters)
            {
                if (parameter.Identifier.Text == "_")
                {
                    Console.WriteLine("Discard parameter detected in parenthesized lambda expression --> 9.0");
                    var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 9.0);
                    entry.Values["Lambda discard parameters"] += 1;
                    break;
                }
            }
        }
    }

    static void IsNativeSizedIntegers(SyntaxNode node, CommitResults card)
    {
        if (node is VariableDeclarationSyntax variableDeclaration)
        {
            foreach (var variable in variableDeclaration.Variables)
            {
                if (variableDeclaration.Type.ToString() == "nint" || variableDeclaration.Type.ToString() == "nuint")
                {
                    Console.WriteLine($"Native-sized integer detected --> 9.0");
                    var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 9.0);
                    entry.Values["Native-sized integers"] += 1;
                }
            }
        }
    }
    static void IsAttributeOnLocalFunction(SyntaxNode node, CommitResults card)
    {
        if (node is LocalFunctionStatementSyntax localFunction1)
        {
            if (localFunction1.AttributeLists.Count > 0)
            {
                Console.WriteLine($"Local function '{localFunction1.Identifier}' has attributes -->  9.0");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 9.0);
                entry.Values["Attributes on local functions"] += 1;
            }
        }
    }
    static void IsFunctionPointer(SyntaxNode node, CommitResults card)
    {
        if (node is FunctionPointerTypeSyntax functionPointerType)
        {
            Console.WriteLine($"Function pointer detected --> 9.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 9.0);
            entry.Values["Function pointers"] += 1;
        }
    }
    static void IsStaticLambda(SyntaxNode node, CommitResults card)
    {
        if (node is ParenthesizedLambdaExpressionSyntax lambda && lambda.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            Console.WriteLine($"Static lambda detected");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 9.0);
            entry.Values["Static lambdas"] += 1;
        }
        if (node is SimpleLambdaExpressionSyntax simpleLambda1 && simpleLambda1.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            Console.WriteLine($"Static lambda detected --> 9.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 9.0);
            entry.Values["Static lambdas"] += 1;
        }
    }
    static void IsRecordType(SyntaxNode node, CommitResults card)
    {
        if (node is RecordDeclarationSyntax recordDeclaration)
        {
            Console.WriteLine($"Record detected --> 9.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 9.0);
            entry.Values["Records"] += 1;
        }
    }
    static void IsTargetedTypeExpressions(SyntaxNode node, CommitResults card)
    {
        // C# 9.0: Targeted type expressions
        if (node is ConditionalExpressionSyntax conditionalExpression1)
        {
            // Check if the conditional expression uses target-typed conditional expression syntax
            if (conditionalExpression1.Condition is LiteralExpressionSyntax &&
                conditionalExpression1.WhenTrue is LiteralExpressionSyntax &&
                conditionalExpression1.WhenFalse is LiteralExpressionSyntax)
            {
                Console.WriteLine("Target-typed conditional expression detected --> 9.0");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 9.0);
                entry.Values["Targeted type expressions"] += 1;
            }
        }
    }
    static void IsCovariantReturn(SyntaxNode node, CommitResults card, SemanticModel model)
    {
        if (node is MethodDeclarationSyntax method1)
        {
            // Get the symbol for the method
            var methodSymbol = model.GetDeclaredSymbol(method1);
            //Console.WriteLine($"{methodSymbol}");

            // Check if the method overrides a base method
            var baseMethodSymbol = methodSymbol?.OverriddenMethod;
            // Console.WriteLine($"{baseMethodSymbol}");
            if (baseMethodSymbol != null && methodSymbol != null)
            {
                // Console.WriteLine($"{methodSymbol}");
                // Console.WriteLine($"{methodSymbol.OverriddenMethod}");
                // Console.WriteLine($"{baseMethodSymbol}");
                if (methodSymbol.OverriddenMethod == baseMethodSymbol && methodSymbol.ReturnType == baseMethodSymbol.ReturnType)
                {
                    Console.WriteLine($"Covariant return detected in method --> 9.0");
                    var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 9.0);
                    entry.Values["Covariant returns"] += 1;
                }
            }
        }
    }
    static void IsGetEnumeratorRecognitionInForeach(SyntaxNode node, CommitResults card, SemanticModel model)
    {
        // C# 9.0: Extension GetEnumerator Recognition in foreach
        // Check if the node is a foreach loop
        if (node is ForEachStatementSyntax foreachSyntax)
        {
            // Get the type of the collection being iterated over
            var collectionType = model.GetTypeInfo(foreachSyntax.Expression).Type;

            if (collectionType != null)
            {
                // Check if there are any extension methods providing GetEnumerator() for the collection type
                var extensions = GetExtensionMethods(model, collectionType, "GetEnumerator");
                //Console.WriteLine($"{extensions.Count()}");
                if (extensions.Any())
                {
                    Console.WriteLine("Foreach loop using extension GetEnumerator --> 9.0");
                    var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 9.0);
                    entry.Values["Extension GetEnumerator Recognition in foreach"] += 1;
                }
            }
        }
    }
    static void ISModuleInitializers(SyntaxNode node, CommitResults card)
    {
        if (node is MethodDeclarationSyntax methodSyntax)
        {
            // Check if the method has ModuleInitializerAttribute
            if (HasModuleInitializerAttribute(methodSyntax))
            {
                Console.WriteLine($"Module initializer found --> 9.0");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 9.0);
                entry.Values["Module initializers"] += 1;
            }
        }
    }
    static void IsTopLevelStatement(SyntaxNode node, CommitResults card)
    {
        // C# 9.0: Top level statements
        // Check if the node represents a global statement (top-level statement)
        if (node is GlobalStatementSyntax globalStatement)
        {
            Console.WriteLine("Top-level statement found --> 9.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 9.0);
            entry.Values["Top level statements"] += 1;
        }
    }

    //---------------------10.0--------------------


    static void IsRecordStructDeclaration(SyntaxNode node, CommitResults card)
    {
        // C# 10.0: record struct declaration
        if (node is RecordDeclarationSyntax recordDeclaration1 &&
            recordDeclaration1.Kind() == SyntaxKind.RecordStructDeclaration)
        {
            Console.WriteLine($"Record struct found --> 10.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 10.0);
            entry.Values["record struct declaration"] += 1;
        }
    }
    static void IsGlobalUsing(SyntaxNode node, CommitResults card)
    {
        // C# 10.0: global using
        if (node is UsingDirectiveSyntax usingDirective && usingDirective.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword))
        {
            Console.WriteLine($"Global using directive found --> 10.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 10.0);
            entry.Values["global using"] += 1;
        }
    }
    static void IsConstantInterpolatedStrings(SyntaxNode node, CommitResults card)
    {
        // C# 10.0: Constant interpolated strings
        if (node is FieldDeclarationSyntax fieldDeclaration1 && fieldDeclaration1.Modifiers.Any(SyntaxKind.ConstKeyword))
        {
            // Check if the field has an interpolated string initializer
            foreach (var variable in fieldDeclaration1.Declaration.Variables)
            {
                if (variable.Initializer?.Value is InterpolatedStringExpressionSyntax interpolatedString1)
                {
                    Console.WriteLine($"Constant interpolated string found --> 10.0");
                    var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 10.0);
                    entry.Values["Constant interpolated strings"] += 1;
                }
            }
        }
    }
    static void IsRecordsWithSealedBaseToStringOverride(SyntaxNode node, CommitResults card)
    {
        // C# 10.0: Records with sealed base toString override
        if (node is RecordDeclarationSyntax recordDeclaration2)
        {
            // Iterate over the members of the record declaration
            foreach (var member in recordDeclaration2.Members)
            {
                // Check if the member is a method declaration for ToString
                if (member is MethodDeclarationSyntax methodDeclaration2 &&
                    methodDeclaration2.Identifier.Text == "ToString" &&
                    methodDeclaration2.Modifiers.Any(SyntaxKind.SealedKeyword) &&
                    methodDeclaration2.Modifiers.Any(SyntaxKind.OverrideKeyword))
                {
                    Console.WriteLine($"Sealed ToString method found in record --> 10.0");
                    var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 10.0);
                    entry.Values["Records with sealed base toString override"] += 1;
                }
            }
        }

    }
    static void IsMixOfDeclarationsAndVariablesInTupleDeconstruction(SyntaxNode node, CommitResults card)
    {
        // C# 10.0: Mix Declarations and Variables in Deconstruction (only for tuples)
        if (node is AssignmentExpressionSyntax assignment && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
        {
            if (assignment.Left is TupleExpressionSyntax tuple)
            {
                bool hasDeclaration = false;
                bool hasIdentifier = false;

                foreach (var element in tuple.Arguments)
                {
                    hasDeclaration = element.Expression is DeclarationExpressionSyntax | false;
                    hasIdentifier = element.Expression is IdentifierNameSyntax | false;
                }

                if (hasDeclaration && hasIdentifier)
                {
                    Console.WriteLine("Mixed declarations and variables found in deconstruction --> 10.0");
                    var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 10.0);
                    entry.Values["Mix Declarations and Variables in Deconstruction (only for tuples)"] += 1;
                }
            }
        }
    }
    static void IsAllowAsyncMethodBuilderDecorator(SyntaxNode node, CommitResults card)
    {
        // C# 10.0: Allow [AsyncMethodBuilder(...)] on methods
        // Check if the node is a method declaration
        if (node is MethodDeclarationSyntax methodDeclaration3)
        {
            // Check if the method has an attribute list
            foreach (var attributeList in methodDeclaration3.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    // Check if the attribute is AsyncMethodBuilder
                    if (attribute.Name.ToString() == "AsyncMethodBuilder" ||
                        attribute.Name is QualifiedNameSyntax qn && qn.Right.Identifier.Text == "AsyncMethodBuilder")
                    {
                        Console.WriteLine($"AsyncMethodBuilder attribute found on method --> 10.0");
                        var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 10.0);
                        entry.Values["Allow [AsyncMethodBuilder(...)] on methods"] += 1;
                    }
                }
            }
        }
    }
    static void IsStaticAbstractMembersInInterfaces(SyntaxNode node, CommitResults card)
    {
        // C# 10.0: Static abstract members in interfaces
        if (node is InterfaceDeclarationSyntax interfaceDeclaration1)
        {
            foreach (var member in interfaceDeclaration1.Members)
            {
                // Check for static abstract methods
                if (member is MethodDeclarationSyntax method2 &&
                    method2.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                    method2.Modifiers.Any(SyntaxKind.AbstractKeyword))
                {
                    Console.WriteLine($"Static abstract method found in interface --> 10.0");
                    var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 10.0);
                    entry.Values["Static abstract members in interfaces"] += 1;
                }

                // Check for static abstract properties
                if (member is PropertyDeclarationSyntax property &&
                    property.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                    property.Modifiers.Any(SyntaxKind.AbstractKeyword))
                {
                    Console.WriteLine($"Static abstract property found in interface --> 10.0");
                    var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 10.0);
                    entry.Values["Static abstract members in interfaces"] += 1;
                }

                // Check for static abstract events (if applicable)
                if (member is EventDeclarationSyntax @event &&
                    @event.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                    @event.Modifiers.Any(SyntaxKind.AbstractKeyword))
                {
                    Console.WriteLine($"Static abstract event found in interface --> 10.0");
                    var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 10.0);
                    entry.Values["Static abstract members in interfaces"] += 1;
                }
            }
        }
    }
    static void IsLambdaImprovement(SyntaxNode node, CommitResults card)
    {
        // C# 10.0: Lambda improvements
        // Check if the node is a lambda expression
        if (node is SimpleLambdaExpressionSyntax simpleLambda2)
        {
            if (CheckLambda(simpleLambda2))
            {
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 10.0);
                entry.Values["Lambda improvements"] += 1;
            }
        }
        else if (node is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
        {
            if (CheckLambda(parenthesizedLambda))
            {
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 10.0);
                entry.Values["Lambda improvements"] += 1;
            }
        }
    }
    static void IsParameterlessStructConstructor(SyntaxNode node, CommitResults card)
    {
        // C# 10.0: Parameterless struct constructors
        if (node is StructDeclarationSyntax structDeclaration1)
        {
            foreach (var member in structDeclaration1.Members)
            {
                // Check if the member is a constructor declaration
                if (member is ConstructorDeclarationSyntax constructorDeclaration)
                {
                    // Check if the constructor has no parameters
                    if (!constructorDeclaration.ParameterList.Parameters.Any())
                    {
                        Console.WriteLine("Parameterless constructor found in struct --> 10.0");
                        var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 10.0);
                        entry.Values["Parameterless struct constructors"] += 1;
                    }
                }
            }
        }
    }
    static void IsCallerExpressionAttribute(SyntaxNode node, CommitResults card)
    {
        // C# 10.0: Caller expression attribute
        if (node is ParameterSyntax parameterSyntax)
        {
            foreach (var attributeList in parameterSyntax.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (attribute.Name.ToString().EndsWith("CallerArgumentExpression"))
                    {
                        Console.WriteLine($"CallerArgumentExpression attribute found on parameter --> 10.0");
                        var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 10.0);
                        entry.Values["Caller expression attribute"] += 1;
                    }
                }
            }
        }
    }

    //---------------------11.0--------------------
    static void IsFileLocalTypes(SyntaxNode node, CommitResults card)
    {
        // C# 11.0: File-local types
        if (node is TypeDeclarationSyntax typeDeclaration)
        {
            if (typeDeclaration.Modifiers.Any(SyntaxKind.FileKeyword))
            {
                Console.WriteLine($"File-local type found --> 11.0");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 11.0);
                entry.Values["File-local types"] += 1;
            }
        }
    }
    static void IsRequiredMembers(SyntaxNode node, CommitResults card)
    {
        // C# 11.0: Required members
        if (node is PropertyDeclarationSyntax propertyDeclaration2)
        {
            if (propertyDeclaration2.Modifiers.Any(SyntaxKind.RequiredKeyword))
            {
                Console.WriteLine($"Required property found --> 11.0");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 11.0);
                entry.Values["Required members"] += 1;
            }
        }
        else if (node is FieldDeclarationSyntax fieldDeclaration)
        {
            if (fieldDeclaration.Modifiers.Any(SyntaxKind.RequiredKeyword))
            {
                foreach (var variable in fieldDeclaration.Declaration.Variables)
                {
                    Console.WriteLine($"Required field found --> 11.0");
                    var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 11.0);
                    entry.Values["Required members"] += 1;
                }
            }
        }
    }
    static void IsUnsignedRightShift(SyntaxNode node, CommitResults card)
    {
        // C# 11.0: Unsigned Right Shift
        if (node.IsKind(SyntaxKind.UnsignedRightShiftExpression) || node.IsKind(SyntaxKind.UnsignedRightShiftAssignmentExpression))
        {
            Console.WriteLine($"Unsigned right shift expression found --> 11.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 11.0);
            entry.Values["Unsigned Right Shift"] += 1;
        }
    }
    static void IsUtf8StringLiterals(SyntaxNode node, CommitResults card)
    {
        // C# 11.0: Utf8 String Literals
        if (node is LiteralExpressionSyntax literalExpression2)
        {
            if (literalExpression2.Token.Text.EndsWith("u8"))
            {
                Console.WriteLine($"UTF-8 string literal found --> 11.0");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 11.0);
                entry.Values["Utf8 String Literals"] += 1;
            }
        }
    }
    static void IsPatternMatchingOnReadOnlySpanOfChars(SyntaxNode node, CommitResults card, SemanticModel model)
    {
        // C# 11.0: Pattern matching on ReadOnlySpan<char>
        if (node is SwitchStatementSyntax switchStatement1)
        {
            var typeInfo = model.GetTypeInfo(switchStatement1.Expression);

            if (typeInfo.Type != null && typeInfo.Type.ToString() == "System.ReadOnlySpan<char>")
            {
                Console.WriteLine($"Pattern matching on ReadOnlySpan<char> found --> 11.0");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 11.0);
                entry.Values["Pattern matching on ReadOnlySpan<char>"] += 1;
            }
        }
    }
    static void IsCheckedOperator(SyntaxNode node, CommitResults card)
    {
        // C# 11.0: Checked Operators
        if (node is OperatorDeclarationSyntax operatorDeclaration)
        {
            //Console.WriteLine($"{operatorDeclaration.CheckedKeyword.ToString()}");
            if (operatorDeclaration.CheckedKeyword.ToString().Equals("checked"))
            {
                Console.WriteLine($"Checked operator found --> 11.0");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 11.0);
                entry.Values["Checked Operators"] += 1;
            }
        }
    }
    static void IsAutoDefaultStruct(SyntaxNode node, CommitResults card)
    {
        // C# 11.0: auto-default structs
        if (node is StructDeclarationSyntax structDeclaration2)
        {
            bool hasConstructor = false;
            foreach (var member in structDeclaration2.Members)
            {
                if (member is ConstructorDeclarationSyntax)
                {
                    hasConstructor = true;
                    break;
                }
            }

            if (!hasConstructor)
            {
                Console.WriteLine($"Auto-default struct found --> 11.0");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 11.0);
                entry.Values["auto-default structs"] += 1;
            }
        }
    }
    static void IsListPattern(SyntaxNode node, CommitResults card)
    {
        // C# 11.0: List patterns
        if (node is ListPatternSyntax)
        {
            Console.WriteLine($"List pattern found --> 11.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 11.0);
            entry.Values["List patterns"] += 1;
        }
    }
    static void IsRawStringLiteral(SyntaxNode node, CommitResults card)
    {
        // C# 11.0: Raw string literals (Does not detect for interpolated)
        if (node is LiteralExpressionSyntax literalExpression3)
        {
            var token = literalExpression3.Token;
            // Check if the token is a raw string literal
            if (token.Kind() == SyntaxKind.MultiLineRawStringLiteralToken ||
                token.Kind() == SyntaxKind.InterpolatedMultiLineRawStringStartToken)
            {
                Console.WriteLine($"Raw string literal found --> 11.0");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 11.0);
                entry.Values["Raw string literals"] += 1;
            }
        }
    }
    static void IsNameOf(SyntaxNode node, CommitResults card)
    {
        // C# 11.0: nameof(parameter)
        if (node is AttributeSyntax attributeSyntax)
        {
            if (attributeSyntax.ArgumentList != null)
            {
                foreach (var argument in attributeSyntax.ArgumentList.Arguments)
                {
                    if (argument.Expression is InvocationExpressionSyntax invocation2 &&
                        invocation2.Expression is IdentifierNameSyntax identifier &&
                        identifier.Identifier.Text == "nameof")
                    {
                        Console.WriteLine($"nameof usage found in attribute --> 11.0");
                        var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 11.0);
                        entry.Values["nameof"] += 1;
                    }
                }
            }
        }
    }
    static void IsGenericAttribute(SyntaxNode node, CommitResults card)
    {
        // C# 11.0: Generic attributes
        if (node is AttributeSyntax attributeSyntax3)
        {
            // Check if the attribute has type arguments
            if (attributeSyntax3.Name is GenericNameSyntax genericName)
            {
                Console.WriteLine($"Generic attribute found --> 11.0");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 11.0);
                entry.Values["Generic attributes"] += 1;
            }
        }
    }

    //---------------------12.0--------------------

    static void IsRefReadonlyParameter(SyntaxNode node, CommitResults card)
    {
        // C# 12.0: ref readonly parameters
        if (node is MethodDeclarationSyntax methodDeclaration4)
        {
            // Check if the method is partial
            if (methodDeclaration4.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                // Check for ref readonly parameters
                foreach (var parameter in methodDeclaration4.ParameterList.Parameters)
                {
                    if (parameter.Modifiers.Any(SyntaxKind.RefKeyword) &&
                        parameter.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
                    {
                        Console.WriteLine($"Partial method with ref readonly parameter found --> 12.0");
                        var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 12.0);
                        entry.Values["ref readonly parameters"] += 1;
                    }
                }
            }
        }
    }
    static void IsCollectionExpression(SyntaxNode node, CommitResults card)
    {
        // C# 12.0: Collection Expressions
        if (node is CollectionExpressionSyntax)
        {
            Console.WriteLine($"Collection expression found --> 12.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 12.0);
            entry.Values["Collection Expressions"] += 1;
        }
    }
    static void IsInlineArrays(SyntaxNode node, CommitResults card)
    {
        // C# 12.0: Inline arrays
        if (node is StructDeclarationSyntax structDeclaration3)
        {
            // Check for InlineArray attribute
            foreach (var attributeList in structDeclaration3.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (attribute.Name.ToString() == "InlineArray")
                    {
                        Console.WriteLine($"Inline array found in struct --> 12.0");
                        var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 12.0);
                        entry.Values["Inline arrays"] += 1;
                    }
                }
            }
        }
    }
    static void IsNameofAccessingInstanceMembers(SyntaxNode node, CommitResults card)
    {
        // C# 12.0: nameof accessing instance members
        if (node is InvocationExpressionSyntax invocation1)
        {
            // Check if the invoked method is nameof
            if (invocation1.Expression is IdentifierNameSyntax identifierName && identifierName.Identifier.Text == "nameof")
            {
                //var argument = invocation.ArgumentList.Arguments.First();
                Console.WriteLine($"nameof found --> 12.0");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 12.0);
                entry.Values["nameof accessing instance members"] += 1;
            }
        }
    }
    static void IsUsingAliases(SyntaxNode node, CommitResults card)
    {
        // C# 12.0: Using aliases for any type: using directive
        if (node is UsingDirectiveSyntax usingDirective1)
        {
            if (usingDirective1.Alias != null)
            {
                Console.WriteLine($"Alias found --> 12.0");
                var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 12.0);
                entry.Values["Using aliases for any type: using directive"] += 1;
            }
        }
    }
    static void IsPrimaryConstructor(SyntaxNode node, CommitResults card)
    {
        // C# 12.0: Primary Constructors
        if (node is ClassDeclarationSyntax classDeclaration && classDeclaration.ParameterList != null)
        {
            Console.WriteLine($"Primary constructor found in class --> 12.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 12.0);
            entry.Values["Primary Constructors"] += 1;
        }
        if (node is StructDeclarationSyntax structDeclaration4 && structDeclaration4.ParameterList != null)
        {
            Console.WriteLine($"Primary constructor found in struct --> 12.0");
            var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 12.0);
            entry.Values["Primary Constructors"] += 1;
        }
    }
    static void IsLambdaOptionalParameters(SyntaxNode node, CommitResults card)
    {
        // C# 12.0: Lambda optional parameters
        if (node is ParenthesizedLambdaExpressionSyntax lambdaExpression)
        {
            // Check for optional parameters in the lambda expression
            foreach (var parameter in lambdaExpression.ParameterList.Parameters)
            {
                if (parameter.Default != null)
                {
                    Console.WriteLine($"Optional parameter found in lambda --> 12.0");
                    var entry = card.ResultsPerVersion.FirstOrDefault(e => e.Version == 12.0);
                    entry.Values["Lambda optional parameters"] += 1;
                }
            }
        }
    }

    //Tooling
    static bool CheckArguments(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        bool foundPositional = false;

        foreach (var argument in arguments)
        {
            if (argument.NameColon != null)
            {
                // Named argument
                if (foundPositional)
                {
                    // A named argument found after a positional argument
                    return true;
                }
            }
            else
            {
                // Positional argument
                foundPositional = true;
            }
        }
        return false;
    }
    static bool IsRefConditionalExpression(ConditionalExpressionSyntax conditionalExpression)
    {
        // Check if the conditional expression is used in a ref context and not without it
        return (conditionalExpression.WhenTrue is RefExpressionSyntax) && (conditionalExpression.WhenFalse is RefExpressionSyntax);
    }
    static IEnumerable<IMethodSymbol> GetExtensionMethods(SemanticModel semanticModel, ITypeSymbol type, string methodName)
    {
        var methods = semanticModel.Compilation.GetSymbolsWithName(methodName, SymbolFilter.Member)
            .OfType<IMethodSymbol>()
            .Where(m => m.IsExtensionMethod &&
                        m.Parameters.Length == 1 &&
                        m.Parameters[0].Type.Equals(type));

        return methods;
    }

    static bool HasModuleInitializerAttribute(MethodDeclarationSyntax methodSyntax)
    {
        return methodSyntax.AttributeLists
            .SelectMany(attrList => attrList.Attributes)
            .Any(attr => attr.Name.ToString() == "ModuleInitializer");
    }

    static bool CheckLambda(LambdaExpressionSyntax lambda)
    {
        // Check for explicit return type by looking at the body
        if (lambda is ParenthesizedLambdaExpressionSyntax ple && ple.ReturnType != null)
        {
            Console.WriteLine("Lambda with explicit return type found --> 10.0");
            return true;
        }

        // Check for attributes on the lambda
        if (lambda.AttributeLists.Count > 0)
        {
            Console.WriteLine("Lambda with attributes found --> 10.0");
            return true;
        }

        // Check the inferred type context (not directly detectable in the AST)
        // This would usually be done through semantic analysis
        return false;
    }


}



public class ResultsEntry
{
    public Dictionary<string, int> Values { get; set; }
    public double Version { get; set; } = 0.0;
}

public class CommitResults
{
    public List<ResultsEntry> ResultsPerVersion { get; set; } = new();
    public string Date { get; set; }

}

public static class CommitFactory
{
    public static CommitResults BuildCommitCard(string date)
    {
        var commit = new CommitResults
        {
            ResultsPerVersion = new List<ResultsEntry>(),
            Date = date
        };

        //Version 7.1
        var results7_1 = new ResultsEntry
        {
            Version = 7.1,
            Values = new Dictionary<string, int>
            {
                { "async Main", 0 },
                { "Default expressions", 0 },
                { "Inffered tuple element names", 0 },
                { "Pattern matching with generics", 0 }
            }
        };
        commit.ResultsPerVersion.Add(results7_1);

        //Version 7.2
        var results7_2 = new ResultsEntry
        {
            Version = 7.2,
            Values = new Dictionary<string, int>
            {
                { "private protected combination", 0 },
                { "Non trailing arguments", 0 },
                { "Digit separator after 0b or 0x", 0 },
                { "conditional ref", 0 },
                { "IN parameters", 0 },
                { "IN arguments", 0 },
                { "ref struct", 0 },
                { "reaonly struct", 0 },
                { "in", 0 },
                { "span<T> and readonlySpan", 0 },
                { "ref", 0 },
                { "Tuple equality", 0 },

            }
        };
        commit.ResultsPerVersion.Add(results7_2);

        //Version 7.3
        var results7_3 = new ResultsEntry
        {
            Version = 7.3,
            Values = new Dictionary<string, int>
            {
                { "Unmanaged type constraint", 0 },
                { "Support == and != for tuples", 0 },
                { "C# 7.3: Auto-Implemented Property Field-Targeted Attributes", 0 },
                { "Stackalloc array initializers", 0 },
                { "fixed-sized buffers", 0 },
                { "Expression variables in initializers", 0 }

            }
        };
        commit.ResultsPerVersion.Add(results7_3);

        //Version 8.0
        var results8_0 = new ResultsEntry
        {
            Version = 8.0,
            Values = new Dictionary<string, int>
            {
                { "Readonly Property", 0 },
                { "Readonly Method", 0 },
                { "IndexType", 0 },
                { "RangeType", 0 },
                { "Static local functions", 0 },
                { "Stackalloc in nested contexts", 0 },
                { "Alternative interpolated verbatim strings", 0 },
                { "Ranges: System.Index and System.Range, and ^", 0 },
                { "Enhanced using", 0 },
                { "Recursive Pattern Matching", 0 },
                { "default interface methods", 0 }
            }
        };
        commit.ResultsPerVersion.Add(results8_0);

        //Version 9.0
        var results9_0 = new ResultsEntry
        {
            Version = 9.0,
            Values = new Dictionary<string, int>
            {
                { "Target-typed new expression", 0 },
                { "SkipLocalsInit Attribute", 0 },
                { "Lambda discard parameters", 0 },
                { "Native-sized integers", 0 },
                { "Attributes on local functions", 0 },
                { "Function pointers", 0 },
                { "Static lambdas", 0 },
                { "Records", 0 },
                { "Targeted type expressions", 0 },
                { "Covariant returns", 0 },
                { "Extension GetEnumerator Recognition in foreach", 0 },
                { "Module initializers", 0 },
                { "Top level statements", 0 }
            }
        };
        commit.ResultsPerVersion.Add(results9_0);

        //Version 10.0
        var results10_0 = new ResultsEntry
        {
            Version = 10.0,
            Values = new Dictionary<string, int>
            {
                { "record struct declaration", 0 },
                { "global using", 0 },
                { "Constant interpolated strings", 0 },
                { "Records with sealed base toString override", 0 },
                { "Mix Declarations and Variables in Deconstruction (only for tuples)", 0 },
                { "Allow [AsyncMethodBuilder(...)] on methods", 0 },
                { "Static abstract members in interfaces", 0 },
                { "Lambda improvements", 0 },
                { "File-scoped namespace", 0 },
                { "Parameterless struct constructors", 0 },
                { "Caller expression attribute", 0 }
            }
        };
        commit.ResultsPerVersion.Add(results10_0);

        //Version 11.0
        var results11_0 = new ResultsEntry
        {
            Version = 11.0,
            Values = new Dictionary<string, int>
            {
                { "File-local types", 0 },
                { "Required members", 0 },
                { "Unsigned Right Shift", 0 },
                { "Utf8 String Literals", 0 },
                { "Pattern matching on ReadOnlySpan<char>", 0 },
                { "Checked Operators", 0 },
                { "auto-default structs", 0 },
                { "List patterns", 0 },
                { "Raw string literals", 0 },
                { "nameof", 0 },
                { "Generic attributes", 0 }
            }
        };
        commit.ResultsPerVersion.Add(results11_0);

        //Version 12.0
        var results12_0 = new ResultsEntry
        {
            Version = 12.0,
            Values = new Dictionary<string, int>
            {
                { "ref readonly parameters", 0 },
                { "Collection Expressions", 0 },
                { "Inline arrays", 0 },
                { "nameof accessing instance members", 0 },
                { "Using aliases for any type: using directive", 0 },
                { "Primary Constructors", 0 },
                { "Lambda optional parameters", 0 }
            }
        };

        commit.ResultsPerVersion.Add(results12_0);
        return commit;
    }
}
