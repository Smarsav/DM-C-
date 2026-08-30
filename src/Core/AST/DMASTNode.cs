using System;
using System.Collections.Generic;
using DMToCSharp.Core;

namespace DMToCSharp.Core.AST
{
    public abstract class DMASTNode
    {
        public Location Location { get; set; }

        protected DMASTNode(Location location)
        {
            Location = location;
        }

        public abstract void Accept(IDMASTVisitor visitor);
        public abstract T Accept<T>(IDMASTVisitor<T> visitor);
    }

    public interface IDMASTVisitor
    {
        void Visit(DMASTFile node);
        void Visit(DMASTObjectDefinition node);
        void Visit(DMASTVarDefinition node);
        void Visit(DMASTProcDefinition node);
        void Visit(DMASTProcParameter node);

        // Statements
        void Visit(DMASTBlock node);
        void Visit(DMASTExpressionStatement node);
        void Visit(DMASTVarDeclarationStatement node);
        void Visit(DMASTIfStatement node);
        void Visit(DMASTWhileStatement node);
        void Visit(DMASTDoWhileStatement node);
        void Visit(DMASTForStandardStatement node);
        void Visit(DMASTForInStatement node);
        void Visit(DMASTForRangeStatement node);
        void Visit(DMASTSwitchStatement node);
        void Visit(DMASTCaseClause node);
        void Visit(DMASTReturnStatement node);
        void Visit(DMASTBreakStatement node);
        void Visit(DMASTContinueStatement node);
        void Visit(DMASTSpawnStatement node);
        void Visit(DMASTTryCatchStatement node);
        void Visit(DMASTDelStatement node);
        void Visit(DMASTGotoStatement node);
        void Visit(DMASTLabelStatement node);

        // Expressions
        void Visit(DMASTConstantNull node);
        void Visit(DMASTConstantNumber node);
        void Visit(DMASTConstantString node);
        void Visit(DMASTConstantResource node);
        void Visit(DMASTConstantPath node);
        void Visit(DMASTIdentifier node);
        void Visit(DMASTUnaryExpression node);
        void Visit(DMASTBinaryExpression node);
        void Visit(DMASTAssignExpression node);
        void Visit(DMASTCallExpression node);
        void Visit(DMASTSuperCallExpression node);
        void Visit(DMASTMemberAccessExpression node);
        void Visit(DMASTIndexAccessExpression node);
        void Visit(DMASTNewExpression node);
        void Visit(DMASTTernaryExpression node);
        void Visit(DMASTListExpression node);
        void Visit(DMASTInterpolatedString node);
        void Visit(DMASTLocateExpression node);
        void Visit(DMASTInputExpression node);
    }

    public interface IDMASTVisitor<T>
    {
        T Visit(DMASTFile node);
        T Visit(DMASTObjectDefinition node);
        T Visit(DMASTVarDefinition node);
        T Visit(DMASTProcDefinition node);
        T Visit(DMASTProcParameter node);

        // Statements
        T Visit(DMASTBlock node);
        T Visit(DMASTExpressionStatement node);
        T Visit(DMASTVarDeclarationStatement node);
        T Visit(DMASTIfStatement node);
        T Visit(DMASTWhileStatement node);
        T Visit(DMASTDoWhileStatement node);
        T Visit(DMASTForStandardStatement node);
        T Visit(DMASTForInStatement node);
        T Visit(DMASTForRangeStatement node);
        T Visit(DMASTSwitchStatement node);
        T Visit(DMASTCaseClause node);
        T Visit(DMASTReturnStatement node);
        T Visit(DMASTBreakStatement node);
        T Visit(DMASTContinueStatement node);
        T Visit(DMASTSpawnStatement node);
        T Visit(DMASTTryCatchStatement node);
        T Visit(DMASTDelStatement node);
        T Visit(DMASTGotoStatement node);
        T Visit(DMASTLabelStatement node);

        // Expressions
        T Visit(DMASTConstantNull node);
        T Visit(DMASTConstantNumber node);
        T Visit(DMASTConstantString node);
        T Visit(DMASTConstantResource node);
        T Visit(DMASTConstantPath node);
        T Visit(DMASTIdentifier node);
        T Visit(DMASTUnaryExpression node);
        T Visit(DMASTBinaryExpression node);
        T Visit(DMASTAssignExpression node);
        T Visit(DMASTCallExpression node);
        T Visit(DMASTSuperCallExpression node);
        T Visit(DMASTMemberAccessExpression node);
        T Visit(DMASTIndexAccessExpression node);
        T Visit(DMASTNewExpression node);
        T Visit(DMASTTernaryExpression node);
        T Visit(DMASTListExpression node);
        T Visit(DMASTInterpolatedString node);
        T Visit(DMASTLocateExpression node);
        T Visit(DMASTInputExpression node);
    }
}
