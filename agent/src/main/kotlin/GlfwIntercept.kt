package dev.byzann.hyperlaunch.agent

import org.objectweb.asm.ClassReader
import org.objectweb.asm.ClassWriter
import java.lang.instrument.ClassFileTransformer
import java.security.ProtectionDomain

class GlfwIntercept : ClassFileTransformer {
    public override fun transform(
        loader: ClassLoader?, className: String?,
        classBeingRedefined: Class<*>?, domain: ProtectionDomain?,
        classfileBuffer: ByteArray?
    ): ByteArray? {
        if (GLFW_CLASS != className) return null // fast reject

        try {
            val reader: ClassReader = ClassReader(classfileBuffer)
            val writer: ClassWriter = ClassWriter(
                reader, ClassWriter.COMPUTE_FRAMES or ClassWriter.COMPUTE_MAXS
            )
            reader.accept(
                GlfwClassVisitor(writer, className),
                ClassReader.EXPAND_FRAMES
            )
            return writer.toByteArray()
        } catch (e: Exception) {
            System.err.println("[LauncherAgent] Transform failed: " + e.message)
            return null // fail safe — use original bytecode
        }
    }
}