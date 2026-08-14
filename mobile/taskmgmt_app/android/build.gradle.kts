allprojects {
    repositories {
        google()
        mavenCentral()
    }
}

// Một số plugin bên thứ 3 (vd file_picker) đóng gói sẵn compileSdk cũ hơn compileSdk của app,
// gây lỗi "requires compile against version X or later" khi dependency của chúng đã cập nhật
// compileSdk mới hơn. Ép compileSdk của mọi module thư viện khớp với compileSdk của app thay vì
// chờ từng plugin tự cập nhật.
subprojects {
    afterEvaluate {
        plugins.withId("com.android.library") {
            extensions.configure<com.android.build.gradle.LibraryExtension> {
                compileSdk = 36
            }
        }
    }
}

val newBuildDir: Directory =
    rootProject.layout.buildDirectory
        .dir("../../build")
        .get()
rootProject.layout.buildDirectory.value(newBuildDir)

subprojects {
    val newSubprojectBuildDir: Directory = newBuildDir.dir(project.name)
    project.layout.buildDirectory.value(newSubprojectBuildDir)
}
subprojects {
    project.evaluationDependsOn(":app")
}

tasks.register<Delete>("clean") {
    delete(rootProject.layout.buildDirectory)
}
