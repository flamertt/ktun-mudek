import { Calculator, ChevronRight, GraduationCap, Layers, ListChecks } from 'lucide-react'
import { useMemo } from 'react'
import { useNavigate, useParams } from 'react-router-dom'

import sectionStyles from '@shared/ui/page-section/PageSection.module.css'
import { PageSection } from '@shared/ui/page-section/PageSection.jsx'
import styles from './CourseEvaluationPage.module.css'
import { useCourseEvaluationMudekData } from '../model/useCourseEvaluationMudekData'

export function CourseEvaluationPage() {
  const { offeringId } = useParams()
  const navigate = useNavigate()
  const d = useCourseEvaluationMudekData(offeringId)

  const nav = useMemo(
    () => [
      {
        title: 'Öğrenci sonuçları',
        desc: 'Başarı setleri + en iyi/en kötü/ortalama öğrenci + tablo.',
        to: `/evaluations/${offeringId}/mudek/students`,
      },
      {
        title: 'Sınav özetleri',
        desc: 'Katılımcı ve toplam puan istatistikleri.',
        to: `/evaluations/${offeringId}/mudek/exams`,
      },
      {
        title: 'Soru / bileşen başarıları',
        desc: 'Soru veya bileşen bazında başarı oranı.',
        to: `/evaluations/${offeringId}/mudek/question-components`,
      },
      {
        title: 'DOC → CLO katkı matrisi',
        desc: 'WeightedAchievement (eşleme × başarı).',
        to: `/evaluations/${offeringId}/mudek/item-clo`,
      },
      {
        title: 'CLO sonuçları',
        desc: 'CLO seviyesinde test + anket bileşenleri.',
        to: `/evaluations/${offeringId}/mudek/clo`,
      },
      {
        title: 'Program çıktısı sonuçları',
        desc: 'PÇ seviyesinde nihai başarı skoru.',
        to: `/evaluations/${offeringId}/mudek/program-outcomes`,
      },
    ],
    [offeringId],
  )

  return (
    <PageSection title={d.title} description="Değerlendirme (dashboard)" error={d.error} loading={d.loading}>
      <div className={styles.layout}>
        <p className={styles.headerLine}>
          {d.courseDetail?.termName ?? d.courseDetail?.TermName ? `${d.courseDetail?.termName ?? d.courseDetail?.TermName} · ` : ''}
          {d.courseDetail?.section ?? d.courseDetail?.Section ? `Şube ${d.courseDetail?.section ?? d.courseDetail?.Section} · ` : ''}
          {d.students?.length ? `${d.students.length} kayıtlı öğrenci` : '—'}
        </p>

        <div className={styles.pageGrid}>
          <div className={styles.mainCol}>
            {d.evaluationId ? (
              <section className={styles.examsHero} aria-labelledby="exams-hero-title">
                <div className={styles.examsHeroAccent} aria-hidden />
                <div className={styles.examsHeroInner}>
                  <div className={styles.examsHeroIcon}>
                    <GraduationCap size={28} strokeWidth={2.2} aria-hidden />
                  </div>
                  <div className={styles.examsHeroBody}>
                    <h3 id="exams-hero-title" className={styles.examsHeroTitle}>
                      Sınavlar ve ölçme yapısı
                    </h3>
                    <p className={styles.examsHeroDesc}>
                      Bu bölümde sınavları tanımlayıp ağırlıkları ayarlarsınız. Her sınav için sorular, yazılı soru–DÖÇ
                      eşlemeleri, öğrenci cevapları; ayrıca ölçme bileşenleri, DÖÇ eşlemeleri ve not girişi sayfalarına
                      buradan geçilir.
                    </p>
                    <ul className={styles.examsHeroList}>
                      <li>
                        <ListChecks size={15} className={styles.examsHeroLiIcon} aria-hidden />
                        Sınav listesi → soru yönetimi ve öğrenci cevapları
                      </li>
                      <li>
                        <Layers size={15} className={styles.examsHeroLiIcon} aria-hidden />
                        Aynı sınavda ölçme bileşenleri ve öğrenci notları
                      </li>
                    </ul>
                    <button
                      type="button"
                      className={styles.examsHeroCta}
                      onClick={() => navigate(`/evaluations/${offeringId}/evaluation/${d.evaluationId}/exams`)}
                    >
                      Sınavları yönet
                      <ChevronRight size={20} aria-hidden />
                    </button>
                  </div>
                </div>
              </section>
            ) : null}

            <div className={styles.panel}>
              <h3 className={styles.panelTitle}>MÜDEK tabloları</h3>
              <p className={styles.panelHint}>
                Tablolar ayrı sayfalarda. İlgili karta basarak detay sayfaya gidebilirsin.
              </p>

              <div className={styles.quickGrid}>
                {nav.map((t) => (
                  <div key={t.to} className={styles.quickCard}>
                    <h4 className={styles.quickTitle}>{t.title}</h4>
                    <p className={styles.quickDesc}>{t.desc}</p>
                    <button type="button" className={styles.goBtn} onClick={() => navigate(t.to)}>
                      Tabloya git
                    </button>
                  </div>
                ))}
              </div>
            </div>

            {d.mudekLoading && !d.mudekResults ? (
              <p className={sectionStyles.muted} style={{ marginTop: '0.25rem' }}>
                MÜDEK sonuçları yükleniyor…
              </p>
            ) : null}
          </div>

          <div className={styles.sideCol}>
            <div className={styles.panel}>
              <h3 className={styles.panelTitle}>Durum</h3>
              <p className={styles.summaryLine}>
                Son hesaplama:{' '}
                {d.mudekMeta?.lastCalculatedAt ? d.formatDate(d.mudekMeta.lastCalculatedAt) : '—'}
              </p>
              {d.mudekMeta?.isCalculationDirty ? (
                <p className={styles.dirtyHint}>
                  Notlar veya yapı değişti; tablolar eski olabilir. Aşağıdan yeniden hesaplayın.
                </p>
              ) : null}
              <button
                type="button"
                className={styles.calcBtn}
                disabled={Boolean(d.loading || !offeringId || d.mudekCalcRunning)}
                onClick={() => void d.runMudekCalculation()}
              >
                <Calculator size={18} aria-hidden />
                {d.mudekCalcRunning ? 'Hesaplanıyor…' : 'MÜDEK sonuçlarını hesapla'}
              </button>
              {d.mudekError ? (
                <p className={sectionStyles.error} role="alert" style={{ margin: 0, fontSize: '0.85rem' }}>
                  {d.mudekError}
                </p>
              ) : null}
            </div>

            {d.cloPoMap?.length ? (
              <div className={styles.panel}>
                <h3 className={styles.panelTitle}>CLO ↔ PÇ matrisi (üniversite)</h3>
                <p className={styles.panelHint}>
                  Bu dersin katalog numarasına göre üniversite kaynaklı DÖÇ–program çıktısı ağırlıkları (ilk 15 satır).
                </p>
                <ul style={{ margin: '0.35rem 0 0', paddingLeft: '1.15rem', fontSize: '0.82rem', lineHeight: 1.45 }}>
                  {d.cloPoMap.slice(0, 15).map((row, idx) => {
                    const clo = row.cloId ?? row.CloId
                    const po = row.programOutcomeId ?? row.ProgramOutcomeId
                    const w = row.weight ?? row.Weight
                    return (
                      <li key={`${clo}-${po}-${idx}`}>
                        DÖÇ {clo} → PÇ {po}
                        {w != null ? ` · ağırlık: ${w}` : ''}
                      </li>
                    )
                  })}
                </ul>
                {d.cloPoMap.length > 15 ? (
                  <p className={sectionStyles.muted} style={{ margin: '0.5rem 0 0', fontSize: '0.8rem' }}>
                    … ve {d.cloPoMap.length - 15} kayıt daha
                  </p>
                ) : null}
              </div>
            ) : null}

            {d.evaluation ? (
              <>
                <div className={styles.panel}>
                  <h3 className={styles.panelTitle}>Hızlı erişim</h3>
                  <p className={styles.panelHint}>
                    Harf notu eşikleri lisans programı bazında admin panelinden yönetilir; bu derste program kuralları
                    otomatik uygulanır.
                  </p>
                  <p className={styles.panelHint}>
                    <strong>Sınavlar</strong> ve tüm alt sayfalar için ana sütundaki vurgulu{' '}
                    <strong>Sınavlar ve ölçme yapısı</strong> kutusundaki <strong>Sınavları yönet</strong> düğmesini
                    kullanın.
                  </p>
                </div>

                <div className={styles.panel}>
                  <h3 className={styles.panelTitle}>MÜDEK Snapshot</h3>
                  <p className={styles.panelHint}>
                    Son hesaplanmış MÜDEK değerlendirme sonuçları bu ders açılışı için aşağıda listelenir.
                  </p>
                  <p className={styles.summaryLine}>
                    Snapshot son hesaplama:{' '}
                    {d.mudekMeta?.lastCalculatedAt ? d.formatDate(d.mudekMeta.lastCalculatedAt) : '—'}
                  </p>
                </div>
              </>
            ) : null}
          </div>
        </div>
      </div>
    </PageSection>
  )
}

