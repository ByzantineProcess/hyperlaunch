package dev.byzann.hyperlaunch.agent

import org.objectweb.asm.ClassVisitor
import org.objectweb.asm.FieldVisitor
import org.objectweb.asm.MethodVisitor
import org.objectweb.asm.Opcodes

const val GLFW_CLASS = "org/lwjgl/glfw/GLFW"
const val DISPLAY_CLASS = "org/lwjgl/opengl/Display"

class GlfwClassVisitor(val classVisitor: ClassVisitor, private val className: String) : ClassVisitor(Opcodes.ASM9, classVisitor) {

    override fun visitMethod(
        access: Int,
        name: String?,
        descriptor: String?,
        signature: String?,
        exceptions: Array<out String?>?
    ): MethodVisitor? {
        val mv = super.visitMethod(access, name, descriptor, signature, exceptions)
        return if (className == GLFW_CLASS && name == "glfwCreateWindow") {
            GlfwMethodVisitor(mv, access, name, descriptor!!, className)
        } else {
            mv
        }
    }
}