using System;
using System.Collections.Generic;
using System.Linq;
using JSIL;
using JSIL.Ast;
using JSIL.Compiler.Extensibility;
using JSIL.Internal;
using JSIL.Transforms;
using JSIL.Translator;
using Mono.Cecil;

namespace WasmSExprEmitter {
    internal class AstEmitter : JSAstVisitor, IAstEmitter {
        private struct LabelInfo {
            public readonly int GroupIndex;
            public readonly int LabelIndex;

            public LabelInfo (int groupIndex, int labelIndex) {
                GroupIndex = groupIndex;
                LabelIndex = labelIndex;
            }
        }

        public static readonly Dictionary<JSOperator, string> OperatorTable;

        public readonly TypeSystem TypeSystem;
        public readonly Configuration Configuration;
        public readonly JavascriptFormatter Formatter;
        public readonly TypeInfoProvider TypeInfo;
        public readonly TypeReferenceContext ReferenceContext = new TypeReferenceContext();
        public readonly bool IsTopLevel;

        public readonly JSILIdentifier JSIL;

        private readonly Dictionary<string, LabelInfo> Labels = new Dictionary<string, LabelInfo>(StringComparer.InvariantCulture);

        private int NextSyntheticLoopIndex = 4096;

        static AstEmitter () {
            OperatorTable = new Dictionary<JSOperator, string> {
                {JSOperator.Add, "add"},
                {JSOperator.Multiply, "mul"},
                {JSOperator.Subtract, "sub"},
                {JSOperator.Equal, "eq"},
                {JSOperator.EqualLoose, "eq"},
                {JSOperator.NotEqual, "neq"},
                {JSOperator.NotEqualLoose, "neq"},
                {JSOperator.LessThan, "lt{0}"},
                {JSOperator.LessThanOrEqual, "le{0}"},
                {JSOperator.GreaterThan, "gt{0}"},
                {JSOperator.GreaterThanOrEqual, "ge{0}"},
            };
        }

        TypeSystem IAstEmitter.TypeSystem {
            get {
                return TypeSystem;
            }
        }

        TypeReferenceContext IAstEmitter.ReferenceContext {
            get {
                return ReferenceContext;
            }
        }

        public SignatureCacher SignatureCacher {
            get; set;
        }

        public AstEmitter (
            JavascriptFormatter formatter, JSILIdentifier jsil, 
            TypeSystem typeSystem, TypeInfoProvider typeInfoProvider, 
            Configuration configuration, bool isTopLevel
        ) 
            : base() 
        {
            Formatter = formatter;
            TypeSystem = typeSystem;
            TypeInfo = typeInfoProvider;
            Configuration = configuration;
            IsTopLevel = isTopLevel;
            JSIL = jsil;
        }

        private void Comment (string format, params object[] values) {
            Formatter.WriteRaw(";; ");
            Formatter.WriteRaw(format, values);
            Formatter.NewLine();
        }

        public void Emit (JSNode node) {
            Visit(node);
        }

        public void VisitNode (JSStatement statement) {
            Console.WriteLine("AstEmitter Unhandled statement type {0}", statement.GetType());

            Formatter.WriteSExpr("untranslatable." + statement.GetType().Name, lineBreakAfter: true);
        }

        private void EmitBlockInterior (List<JSStatement> body, string blockName = null) {
            while (body.Count == 1) {
                // HACK: Collapse '(loop (block ...) )' into '(loop ...)'
                var bs = body[0] as JSBlockStatement;
                // HACK: Collapse '(block (...))' into '(...)'
                var es = body[0] as JSExpressionStatement;

                if (
                    (bs != null) && 
                    // HACK: Loops are blocks
                    (bs.GetType() == typeof(JSBlockStatement))
                ) {
                    body = bs.Statements;
                } else if (
                    (es != null) && 
                    // FIXME: Is this necessary?
                    ((blockName == null) || (blockName == "block"))
                ) {
                    Visit(es);
                    return;
                } else {
                    break;
                }
            }

            Formatter.WriteSExpr(
                blockName ?? "block", (_) =>
                    EmitArgumentList(_, body, true),
                lineBreakInside: true,
                lineBreakAfter: true
            );
        }

        private void EmitLabelledBlock (string label, Action<JavascriptFormatter> writeBody) {
            if (label == null) {
                writeBody(Formatter);
                return;
            }

            Formatter.WriteSExpr(
                "label", (_) => {
                    _.WriteRaw("${0} ", Util.EscapeIdentifier(label));
                    _.NewLine();
                    writeBody(_);
                },
                lineBreakAfter: true
            );
        }

        private void EmitLabelledBlock (string label, List<JSStatement> body, string blockName = null) {
            EmitLabelledBlock(
                label, (_) =>
                    EmitBlockInterior(body, blockName)
            );
        }

        public void VisitNode (JSBlockStatement block) {
            EmitBlockInterior(block.Statements);            
        }

        private string IndexToLabel (int? index) {
            if (index.HasValue)
                return "loop_" + index.Value;
            else
                return null;
        }

        public void VisitNode (JSNullStatement ns) {
            Formatter.WriteRaw("(nop)");
        }

        private void SynthesizeLoopLabel (JSLoopStatement ls) {
            if (ls.Index == null)
                ls.Index = (NextSyntheticLoopIndex++);
        }

        public void VisitNode (JSForLoop fl) {
            SynthesizeLoopLabel(fl);

            if (fl.Initializer != null) {
                Formatter.ConditionalNewLine();
                Formatter.NewLine();
                Comment("for ({0}; ...)", fl.Initializer);
                Visit(fl.Initializer);
            }

            Formatter.ConditionalNewLine();
            Formatter.NewLine();
            EmitLabelledBlock(
                IndexToLabel(fl.Index),
                (__) =>
                    __.WriteSExpr(
                        "loop",
                        (_) => {
                            Comment("for (...; {0}; ...)", fl.Condition);
                            var synthetic = new JSIfStatement(
                                fl.Condition,
                                new JSNullStatement(),
                                new JSExpressionStatement(new JSBreakExpression { TargetLoop = fl.Index })
                            );
                            Visit(synthetic);

                            _.ConditionalNewLine();
                            _.NewLine();
                            Comment("for (...) {{ ");
                            EmitBlockInterior(fl.Statements);

                            if (fl.Increment != null) {
                                _.ConditionalNewLine();
                                _.NewLine();
                                Comment("for (...; ...; <!>)");
                                Visit(fl.Increment);
                            }
                        },
                        true, true
                    )
            );
        }

        public void VisitNode (JSWhileLoop wl) {
            SynthesizeLoopLabel(wl);

            Formatter.ConditionalNewLine();
            Formatter.NewLine();
            Comment("while ({0})", wl.Condition);
            EmitLabelledBlock(
                IndexToLabel(wl.Index),
                (__) =>
                    __.WriteSExpr(
                        "loop",
                        (_) => {
                            var synthetic = new JSIfStatement(
                                wl.Condition,
                                new JSNullStatement(),
                                new JSExpressionStatement(new JSBreakExpression { TargetLoop = wl.Index })
                            );
                            Visit(synthetic);

                            _.ConditionalNewLine();
                            _.NewLine();
                            Comment("while (...) {{ ");
                            EmitBlockInterior(wl.Statements);
                        },
                        true, true
                    )
            );
        }

        public void VisitNode (JSDoLoop dl) {
            SynthesizeLoopLabel(dl);

            Formatter.ConditionalNewLine();
            Formatter.NewLine();
            Comment("do {{");
            EmitLabelledBlock(
                IndexToLabel(dl.Index),
                (__) =>
                    __.WriteSExpr(
                        "loop",
                        (_) => {
                            EmitBlockInterior(dl.Statements);

                            _.ConditionalNewLine();
                            _.NewLine();
                            Comment("do {{ ... }} while ({0})", dl.Condition);
                            var synthetic = new JSIfStatement(
                                dl.Condition,
                                new JSNullStatement(),
                                new JSExpressionStatement(new JSBreakExpression { TargetLoop = dl.Index })
                            );
                            Visit(synthetic);
                        },
                        true, true
                    )
            );
        }

        public void VisitNode (JSExpressionStatement es) {
            base.VisitNode(es);
        }

        public void VisitNode (JSVariableDeclarationStatement vds) {
            foreach (var declaration in vds.Declarations) {
                if ((declaration.Right == null) || (declaration.Right.IsNull))
                    continue;

                Visit(declaration);
            }
        }

        private LabelInfo GetLabelInfo (string name) {
            return Labels[name];
        }

        private void SetCurrentLabel (string label, int? expectedGroup = null) {
            var labelInfo = GetLabelInfo(label);
            if (
                expectedGroup.HasValue &&
                (expectedGroup.Value != labelInfo.GroupIndex)
            )
                throw new InvalidOperationException(string.Format(
                    "Expected label {0} to be in label group {1} but got {2}",
                    label, expectedGroup, labelInfo.GroupIndex
                ));

            Formatter.ConditionalNewLine();
            Formatter.WriteRaw("(setlocal $currentLabel_{0} (const.i32 {1}))", labelInfo.GroupIndex, labelInfo.LabelIndex);
            Formatter.NewLine();
        }

        public void VisitNode (JSLabelGroupStatement lgs) {
            Formatter.ConditionalNewLine();
            Formatter.NewLine();
            var labelName = "labelgroup_" + lgs.GroupIndex;

            var firstLabel = lgs.Labels.First().Key;

            foreach (var kvp in lgs.Labels)
                Labels.Add(kvp.Key, new LabelInfo(lgs.GroupIndex, Labels.Count));

            Comment("LabelGroup {0} (starting at {1})", lgs.GroupIndex, firstLabel);
            SetCurrentLabel(firstLabel, lgs.GroupIndex);
            Formatter.NewLine();

            Formatter.WriteSExpr(
                "label", (__) => {
                    __.WriteRaw("${0} ", labelName);

                    // HACK
                    __.Unindent();

                    __.WriteSExpr(
                        "loop", (_) => {
                            _.WriteRaw("(label $labelgroup_{0}_dispatch ", lgs.GroupIndex);
                            _.NewLine();

                            foreach (var kvp in lgs.Labels) {
                                var labelInfo = GetLabelInfo(kvp.Key);

                                _.ConditionalNewLine();
                                Comment("Begin Label {1}", lgs.GroupIndex, kvp.Key);

                                _.WriteRaw("(if (eq.i32 (getlocal $currentLabel_{0}) (const.i32 {1})) ", labelInfo.GroupIndex, labelInfo.LabelIndex);
                                _.Indent();
                                _.NewLine();

                                Visit(kvp.Value);
                                _.ConditionalNewLine();
                                _.NewLine();

                                _.Unindent();
                                _.WriteRaw(")");
                                _.NewLine();
                                Comment("End Label {1}", lgs.GroupIndex, kvp.Key);
                                _.NewLine();
                            }

                            _.ConditionalNewLine();
                            Comment("Fallthrough exit from labelgroup {0}", lgs.GroupIndex);
                            _.WriteRaw("(break $labelgroup_{0})", lgs.GroupIndex);
                            _.NewLine();

                            _.ConditionalNewLine();
                            _.WriteRaw(")", lgs.GroupIndex);
                            _.NewLine();
                        }
                    );

                    // HACK
                    __.Indent();
                }
            );
        }

        public void VisitNode (JSIfStatement ifs) {
            Formatter.NewLine();
            Formatter.WriteSExpr(
                "if", (_) =>
                    EmitArgumentList(_, new JSNode[] { ifs.Condition, ifs.TrueClause, ifs.FalseClause }, true),
                lineBreakInside: true,
                lineBreakAfter: true
            );
        }

        public void VisitNode (JSBreakExpression be) {
            int targetLoop;
            if (be.TargetLoop.HasValue)
                targetLoop = be.TargetLoop.Value;
            else
                targetLoop = Stack.OfType<JSLoopStatement>().First().Index.Value;

            Formatter.WriteSExpr(
                "break", (_) => _.WriteRaw("$loop_{0}", be.TargetLoop.Value)
            );
        }

        public void VisitNode (JSContinueExpression ce) {
            int targetLoop;
            if (ce.TargetLoop.HasValue)
                targetLoop = ce.TargetLoop.Value;
            else
                targetLoop = Stack.OfType<JSLoopStatement>().First().Index.Value;

            Formatter.WriteSExpr(
                "break", (_) => _.WriteRaw("$loop_{0}_iterate", ce.TargetLoop.Value)
            );
        }

        public void VisitNode (JSGotoExpression ge) {
            var labelInfo = GetLabelInfo(ge.TargetLabel);

            Formatter.ConditionalNewLine();
            Comment("goto {0}", ge.TargetLabel);
            Formatter.WriteRaw("(block (setlocal $currentLabel_{0} (const.i32 {1})) (break $labelgroup_{0}_dispatch) )", labelInfo.GroupIndex, labelInfo.LabelIndex);
            Formatter.NewLine();
        }

        public void VisitNode (JSExpression expression) {
            Console.WriteLine("AstEmitter Unhandled expression type {0}", expression.GetType());

            Formatter.WriteSExpr("untranslatable." + expression.GetType().Name, lineBreakAfter: IsTopLevel);
        }

        public void VisitNode (JSLiteral literal) {
            var literalType = literal.GetActualType(TypeSystem);
            var typeToken = WasmUtil.PickTypeKeyword(literalType);

            if (typeToken == null) {
                Console.WriteLine("AstEmitter Unhandled literal type {0}", literalType.FullName);

                Formatter.WriteSExpr("untranslatable.literal");
            }

            dynamic literalValue;
            if (literal is JSDefaultValueLiteral) {
                literalValue = 0;
            } else {
                literalValue = (dynamic)literal.Literal;
                if (literalValue is bool)
                    literalValue = (literalValue ? 1 : 0);
            }

            Formatter.WriteSExpr(
                "const." + typeToken,
                // HACK
                (_) => Formatter.Value(literalValue)
            );
        }

        private void Assign (JSExpression target, JSExpression value) {
            var leftVar = target as JSVariable;
            if (leftVar == null) {
                Console.WriteLine("Invalid assignment target {0}", target);
                return;
            }

            Formatter.WriteSExpr(
                "setlocal",
                (_) => {
                    _.WriteRaw("${0} ", Util.EscapeIdentifier(leftVar.Name));
                    Visit(value);
                },
                lineBreakAfter: true
            );
        }

        public void VisitNode (JSVariable variable) {
            Formatter.WriteSExpr(
                "getlocal",
                (_) =>
                    _.WriteRaw("${0}", Util.EscapeIdentifier(variable.Name))
            );
        }

        public void VisitNode (JSBinaryOperatorExpression boe) {
            var boeType = boe.GetActualType(TypeSystem);
            var typeToken = WasmUtil.PickTypeKeyword(boeType);

            if (typeToken == null) {
                Console.WriteLine("Unhandled binary operator type {0}", boeType);
                return;
            }

            if (boe.Operator == JSOperator.Assignment) {
                Assign(boe.Left, boe.Right);
                return;
            }

            string keyword;
            if (!OperatorTable.TryGetValue(boe.Operator, out keyword)) {
                Console.WriteLine("Unimplemented operator {0}", boe.Operator);
                return;
            }

            var leftType = boe.Left.GetActualType(TypeSystem);
            var rightType = boe.Right.GetActualType(TypeSystem);
            var leftSign = TypeUtil.IsSigned(leftType);

            // HACK: Emit the argument type since we're comparing it
            if (boe.Operator is JSComparisonOperator)
                typeToken = WasmUtil.PickTypeKeyword(leftType);

            var signSuffix = "";
            if (leftSign.HasValue && TypeUtil.IsIntegral(leftType)) {
                signSuffix = leftSign.Value
                    ? "s"
                    : "u";
            }

            var actualKeyword = string.Format(
                keyword + "." + typeToken,
                signSuffix
            );

            Formatter.WriteSExpr(
                actualKeyword,
                (_) => EmitArgumentList(_, new[] { boe.Left, boe.Right }, true),
                true, false
            );
        }

        private void EmitArgumentList<TArgs> (JavascriptFormatter formatter, TArgs arguments, bool interiorLineBreak = false)
            where TArgs : IEnumerable<JSNode>
        {
            foreach (var arg in arguments) {
                // FIXME: Should we put something else here?
                if (arg == null)
                    continue;

                Visit(arg);

                if (!formatter.PreviousWasLineBreak) {
                    if (interiorLineBreak)
                        formatter.NewLine();
                    else
                        formatter.Space();
                }
            }
        }

        public void VisitNode (SExpression sexpr) {
            Formatter.WriteSExpr(
                sexpr.Keyword,
                (_) => {
                    sexpr.BeforeWriteValues(this);
                    EmitArgumentList(_, sexpr.Children, sexpr.LineBreakInside);
                    sexpr.AfterWriteValues(this);
                },
                sexpr.LineBreakInside,
                sexpr.LineBreakAfter
            );
        }

        public void VisitNode (JSReferenceExpression re) {
            if (re.Referent.IsConstant)
                Visit(re.Referent);
            else {
                Console.WriteLine("Can't translate {0}", re);
                Formatter.WriteSExpr("untranslatable.reference");
            }
        }

        public void VisitNode (JSReturnExpression ret) {
            Formatter.WriteSExpr("return", (_) =>
                Visit(ret.Value)
            );
        }

        public void VisitNode (JSInvocationExpression ie) {
            var jsm = ie.JSMethod;
            if (jsm == null) {
                Console.WriteLine("Can't translate non-JSMethod {0}", ie);
                Formatter.WriteSExpr("untranslatable.call");
                return;
            } else if (!jsm.Method.IsStatic) {
                Console.WriteLine("Can't translate instance call {0}", ie);
                Formatter.WriteSExpr("untranslatable.call");
                return;
            }

            var methodDef = jsm.Reference.Resolve();
            var memberName = WasmUtil.FormatMemberName(methodDef);

            Formatter.WriteSExpr("call", (_) => {
                _.WriteRaw("${0} ", memberName);
                EmitArgumentList(_, ie.Arguments, false);
            });
        }

        public void VisitNode (JSIntegerToFloatExpression itfe) {
            var type = itfe.GetActualType(TypeSystem);
            var typeToken = WasmUtil.PickTypeKeyword(type);
            var originalType = itfe.Expression.GetActualType(TypeSystem);
            var originalTypeToken = WasmUtil.PickTypeKeyword(originalType);

            Formatter.WriteSExpr(
                "converts." + originalTypeToken + "." + typeToken, (_) => {
                    Visit(itfe.Expression);
                }
            );
        }

        public void VisitNode (AssertEq aseq) {
            Formatter.WriteRaw("(asserteq (invoke ");
            Formatter.Value(aseq.ExportedFunctionName);
            Formatter.Space();
            EmitArgumentList(Formatter, aseq.Arguments, false);
            Formatter.WriteRaw(" ) ");
            Visit(aseq.Expected);
            Formatter.WriteRaw(" )");
            Formatter.NewLine();
        }

        /*
        public void VisitNode (JSInvocationExpression ie) {
        }
        */
    }
}