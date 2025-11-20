namespace SharpLox.Core.Stdlib {
    public class StandardLibrary {
        public static readonly string File = "class File { \n" +
                "init(path) { this.path = path; }\n" +
                "exists() { return fileExists(this.path); }\n" +
                "create() { return createFile(this.path); }\n" +
                "delete() { return deleteFile(this.path); }\n" +
                "read() { return readFile(this.path); }\n" +
                "write(data) { return writeFile(this.path, data); }\n" +
                "append(data) { return appendFile(this.path, data); }\n" +
                "}\n";
    }


}