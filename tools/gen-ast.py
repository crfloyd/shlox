import sys

def define_ast(output_dir, base_name, types):
    path = outputDir + "/" + base_name + ".cs"
    out_file = open(path, 'w')
    out_file.write("using System;\n")
    out_file.write("namespace Lox\n")
    out_file.write("{\n")
    out_file.write(f"    public abstract class {base_name}\n")
    out_file.write("    {\n")
    out_file.write(f"        public abstract T Accept<T>(I{base_name}Visitor<T> visitor);\n")
    out_file.write("    }\n")

    # Define Visitor
    define_visitor(out_file, base_name, types)

    # The AST class implementations
    for t in types:
        parts = t.split(":")
        class_name = parts[0].strip()
        fields = parts[1].strip()
        define_type(out_file, base_name, class_name, fields)

    out_file.write("}\n")
    out_file.close()

def define_visitor(f, base_name, types):
    f.write(f"\n    public interface I{base_name}Visitor<T>\n")
    f.write("    {\n")
    for t in types:
        name = t.split(":")[0].strip()
        f.write(f"          T Visit{name.capitalize()}{base_name}({name.capitalize()} {base_name.lower()});\n")
    f.write("    }\n")

def define_type(file, base_name, class_name, field_list):
    file.write(f"\n    public class {class_name} : {base_name}\n")
    file.write("    {\n")

    # Fields
    fields = field_list.split(", ")
    for field in fields:
        field_parts = field.split(" ")
        field_name = field_parts[1]
        field_type = field_parts[0]
        file.write(f"        public {field_type} {field_name.capitalize()} {{ get; }}\n")

    # Constructor
    file.write(f"\n        public {class_name}({field_list})\n")
    file.write("        {\n")
    for field in fields:
        name = field.split(" ")[1]
        file.write(f"            {name.capitalize()} = {name};\n")
    file.write("        }\n")


    # Visitor implementation
    file.write(f"\n        public override T Accept<T>(I{base_name}Visitor<T> visitor)\n")
    file.write("        {\n")
    file.write(f"            return visitor.Visit{class_name.capitalize()}{base_name}(this);\n")
    file.write("        }\n")

    file.write("    }\n")


if __name__ == "__main__":
    # Expr
    outputDir = "../src"
    with open("expr-defs.txt", 'r') as f:
        data = f.readlines()
        define_ast(outputDir, "Expr", data)

    # Stmt
    with open("stmnt-defs.txt", 'r') as f:
        data = f.readlines()
        define_ast(outputDir, "Stmt", data)
